using System.Net;
using System.Reflection;
using k8s;
using k8s.Autorest;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.OsProbes;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class OsRestartPodsTests
{
    private sealed class TestableOsRestartPods : OsRestartPods
    {
        public void SetClient(Kubernetes kubernetesClient) => Kubernetes = kubernetesClient;

        public void InvokeRunOsProbe() => RunOsProbe();
    }

    [Test]
    public void RunOsProbe_WhenOneLabelLookupFailsAndNoPodsRemain_ShouldReturn()
    {
        using var server = new TestHttpServer();
        server.EnqueueJsonResponse(
            "GET",
            "/api/v1/namespaces/namespace/pods",
            "{\"message\":\"label lookup failed\"}",
            HttpStatusCode.InternalServerError);
        server.EnqueueJsonResponse(
            "GET",
            "/api/v1/namespaces/namespace/pods",
            """
            {
              "apiVersion": "v1",
              "kind": "PodList",
              "items": []
            }
            """);

        var probe = CreateProbe(server.CreateKubernetesClient(), ["app=one", "app=two"]);

        Assert.DoesNotThrow(() => probe.InvokeRunOsProbe());
    }

    [Test]
    public void RunOsProbe_WhenPodsRestartAndBecomeReadyAgain_ShouldDeletePodsAndWaitForRecovery()
    {
        using var server = new TestHttpServer();
        server.EnqueueJsonResponse("GET", "/api/v1/namespaces/namespace/pods", CreatePodListJson("pod-a"));
        server.EnqueueJsonResponse("DELETE", "/api/v1/namespaces/namespace/pods/pod-a", "{}");
        server.EnqueueJsonResponse("GET", "/apis/apps/v1/namespaces/namespace/deployments", CreateDeploymentListJson(true));
        server.EnqueueJsonResponse("GET", "/apis/apps/v1/namespaces/namespace/statefulsets", CreateStatefulSetListJson());
        server.EnqueueJsonResponse("GET", "/apis/apps/v1/namespaces/namespace/deployments", CreateDeploymentListJson(false));
        server.EnqueueJsonResponse("GET", "/apis/apps/v1/namespaces/namespace/statefulsets", CreateStatefulSetListJson());
        server.EnqueueJsonResponse("GET", "/apis/apps/v1/namespaces/namespace/deployments", CreateDeploymentListJson(true));
        server.EnqueueJsonResponse("GET", "/apis/apps/v1/namespaces/namespace/statefulsets", CreateStatefulSetListJson());

        var probe = CreateProbe(server.CreateKubernetesClient(), ["app=worker"]);

        Assert.DoesNotThrow(() => probe.InvokeRunOsProbe());
    }

    [Test]
    public void RunOsProbe_WhenDeleteFailsAndReadinessNeverChanges_ShouldCatchTimeout()
    {
        using var server = new TestHttpServer();
        server.EnqueueJsonResponse("GET", "/api/v1/namespaces/namespace/pods", CreatePodListJson("pod-a"));
        server.EnqueueJsonResponse("DELETE", "/api/v1/namespaces/namespace/pods/pod-a",
            "{\"message\":\"delete failed\"}", HttpStatusCode.InternalServerError);

        for (var i = 0; i < 200; i++)
        {
            server.EnqueueJsonResponse("GET", "/apis/apps/v1/namespaces/namespace/deployments", CreateDeploymentListJson(true));
            server.EnqueueJsonResponse("GET", "/apis/apps/v1/namespaces/namespace/statefulsets", CreateStatefulSetListJson());
        }

        var probe = CreateProbe(server.CreateKubernetesClient(), ["app=worker"], timeoutSeconds: 1,
            intervalBetweenChecksMs: 10);

        Assert.DoesNotThrow(() => probe.InvokeRunOsProbe());
    }

    [Test]
    public void AreDeploymentsReady_WhenLookupFails_ShouldThrowWrappedHttpOperationException()
    {
        using var server = new TestHttpServer();
        server.EnqueueJsonResponse(
            "GET",
            "/apis/apps/v1/namespaces/namespace/deployments",
            "{\"message\":\"deployments failed\"}",
            HttpStatusCode.InternalServerError);

        var probe = CreateProbe(server.CreateKubernetesClient(), ["app=worker"]);
        var exception = Assert.Throws<TargetInvocationException>(() => InvokePrivateMethod(probe, "AreDeploymentsReady"));

        Assert.That(exception!.InnerException, Is.TypeOf<HttpOperationException>());
        Assert.That(exception.InnerException!.Message, Does.Contain("deployments failed"));
    }

    [Test]
    public void AreStatefulSetsReady_WhenReadyReplicasDoNotMatch_ShouldReturnFalse()
    {
        using var server = new TestHttpServer();
        server.EnqueueJsonResponse(
            "GET",
            "/apis/apps/v1/namespaces/namespace/statefulsets",
            """
            {
              "apiVersion": "apps/v1",
              "kind": "StatefulSetList",
              "items": [
                {
                  "metadata": { "name": "stateful-a" },
                  "spec": { "replicas": 2 },
                  "status": { "readyReplicas": 1 }
                }
              ]
            }
            """);

        var probe = CreateProbe(server.CreateKubernetesClient(), ["app=worker"]);
        var result = InvokePrivateMethod(probe, "AreStatefulSetsReady");

        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void AreStatefulSetsReady_WhenLookupFails_ShouldThrowWrappedHttpOperationException()
    {
        using var server = new TestHttpServer();
        server.EnqueueJsonResponse(
            "GET",
            "/apis/apps/v1/namespaces/namespace/statefulsets",
            "{\"message\":\"statefulsets failed\"}",
            HttpStatusCode.InternalServerError);

        var probe = CreateProbe(server.CreateKubernetesClient(), ["app=worker"]);
        var exception = Assert.Throws<TargetInvocationException>(() => InvokePrivateMethod(probe, "AreStatefulSetsReady"));

        Assert.That(exception!.InnerException, Is.TypeOf<HttpOperationException>());
        Assert.That(exception.InnerException!.Message, Does.Contain("statefulsets failed"));
    }

    private static object? InvokePrivateMethod(object target, string methodName)
    {
        var method = typeof(OsRestartPods).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        return method.Invoke(target, null);
    }

    private static TestableOsRestartPods CreateProbe(Kubernetes kubernetesClient, string[] labels,
        int timeoutSeconds = 5, int intervalBetweenChecksMs = 0)
    {
        var probe = new TestableOsRestartPods
        {
            Configuration = new OsRestartPodsConfig
            {
                Openshift = CreateOpenshiftConfig(),
                ApplicationLabels = labels,
                TimeoutWaitForDesiredStateSeconds = timeoutSeconds,
                IntervalBetweenDesiredStateChecksMs = intervalBetweenChecksMs
            },
            Context = Globals.Context
        };
        probe.SetClient(kubernetesClient);
        return probe;
    }

    private static Openshift CreateOpenshiftConfig()
    {
        return new Openshift
        {
            Cluster = "cluster",
            Namespace = "namespace",
            Username = "username",
            Password = "password"
        };
    }

    private static string CreatePodListJson(params string[] podNames)
    {
        var pods = string.Join(",",
            podNames.Select(podName =>
                $$"""
                  {
                    "metadata": { "name": "{{podName}}", "namespace": "namespace" },
                    "spec": { "containers": [ { "name": "worker" } ] }
                  }
                  """));

        return $$"""
                 {
                   "apiVersion": "v1",
                   "kind": "PodList",
                   "items": [{{pods}}]
                 }
                 """;
    }

    private static string CreateDeploymentListJson(bool available)
    {
        var status = available ? "True" : "False";
        return $$"""
                 {
                   "apiVersion": "apps/v1",
                   "kind": "DeploymentList",
                   "items": [
                     {
                       "metadata": { "name": "deployment-a" },
                       "spec": { "replicas": 1 },
                       "status": {
                         "replicas": 1,
                         "availableReplicas": 1,
                         "updatedReplicas": 1,
                         "readyReplicas": 1,
                         "conditions": [
                           { "type": "Available", "status": "{{status}}" }
                         ]
                       }
                     }
                   ]
                 }
                 """;
    }

    private static string CreateStatefulSetListJson()
    {
        return """
               {
                 "apiVersion": "apps/v1",
                 "kind": "StatefulSetList",
                 "items": []
               }
               """;
    }
}
