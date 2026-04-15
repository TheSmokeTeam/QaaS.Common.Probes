using System.Net;
using System.Reflection;
using k8s;
using k8s.Autorest;
using k8s.Models;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.OsProbes;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class OsExecuteCommandsInContainersTests
{
    private sealed class TestableOsExecuteCommandsInContainers : OsExecuteCommandsInContainers
    {
        public List<(string PodName, string ContainerName)> Executions { get; } = [];

        public void SetClient(Kubernetes kubernetesClient) => Kubernetes = kubernetesClient;

        public void InvokeRunOsProbe() => RunOsProbe();

        protected override string ExecuteCommands(V1Pod pod, string containerName)
        {
            Executions.Add((pod.Metadata!.Name!, containerName));
            return $"{pod.Metadata!.Name}:{containerName}";
        }
    }

    [Test]
    public void RunOsProbe_WhenNoPodsMatchConfiguredLabels_ShouldReturnWithoutExecutingCommands()
    {
        using var server = new TestHttpServer();
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

        var probe = new TestableOsExecuteCommandsInContainers
        {
            Configuration = new OsExecuteCommandsInContainersConfig
            {
                Openshift = CreateOpenshiftConfig(),
                ApplicationLabels = ["app=test"],
                Commands = ["echo", "hello"]
            },
            Context = Globals.Context
        };
        probe.SetClient(server.CreateKubernetesClient());

        Assert.DoesNotThrow(() => probe.InvokeRunOsProbe());
        Assert.That(probe.Executions, Is.Empty);
    }

    [Test]
    public void RunOsProbe_WhenContainerNameIsSpecified_ShouldExecuteOnlyMatchingContainersAcrossAllLabels()
    {
        using var server = new TestHttpServer();
        server.EnqueueJsonResponse(
            "GET",
            "/api/v1/namespaces/namespace/pods",
            """
            {
              "apiVersion": "v1",
              "kind": "PodList",
              "items": [
                {
                  "metadata": { "name": "pod-a", "namespace": "namespace" },
                  "spec": {
                    "containers": [
                      { "name": "worker" },
                      { "name": "sidecar" }
                    ]
                  }
                }
              ]
            }
            """);
        server.EnqueueJsonResponse(
            "GET",
            "/api/v1/namespaces/namespace/pods",
            """
            {
              "apiVersion": "v1",
              "kind": "PodList",
              "items": [
                {
                  "metadata": { "name": "pod-b", "namespace": "namespace" },
                  "spec": {
                    "containers": [
                      { "name": "worker" }
                    ]
                  }
                }
              ]
            }
            """);

        var probe = new TestableOsExecuteCommandsInContainers
        {
            Configuration = new OsExecuteCommandsInContainersConfig
            {
                Openshift = CreateOpenshiftConfig(),
                ApplicationLabels = ["app=one", "app=two"],
                Commands = ["echo", "hello"],
                ContainerName = "worker"
            },
            Context = Globals.Context
        };
        probe.SetClient(server.CreateKubernetesClient());

        probe.InvokeRunOsProbe();

        Assert.That(probe.Executions, Is.EqualTo(new[]
        {
            ("pod-a", "worker"),
            ("pod-b", "worker")
        }));
    }

    [Test]
    public void RunOsProbe_WhenPodLookupFails_ShouldThrowWrappedHttpOperationException()
    {
        using var server = new TestHttpServer();
        server.EnqueueJsonResponse(
            "GET",
            "/api/v1/namespaces/namespace/pods",
            "{\"message\":\"lookup failed\"}",
            HttpStatusCode.InternalServerError);

        var probe = new TestableOsExecuteCommandsInContainers
        {
            Configuration = new OsExecuteCommandsInContainersConfig
            {
                Openshift = CreateOpenshiftConfig(),
                ApplicationLabels = ["app=test"],
                Commands = ["echo", "hello"]
            },
            Context = Globals.Context
        };
        probe.SetClient(server.CreateKubernetesClient());

        var exception = Assert.Throws<HttpOperationException>(() => probe.InvokeRunOsProbe());
        Assert.That(exception!.Message, Does.Contain("lookup failed"));
    }

    [Test]
    public void GetExecutionError_WhenStatusPayloadIsSuccess_ReturnsEmpty()
    {
        var method = typeof(OsExecuteCommandsInContainers)
            .GetMethod("GetExecutionError", BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (string)method.Invoke(null, ["{\"metadata\":{},\"status\":\"Success\"}"])!;

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetExecutionError_WhenStatusPayloadIsFailure_ReturnsOriginalPayload()
    {
        var method = typeof(OsExecuteCommandsInContainers)
            .GetMethod("GetExecutionError", BindingFlags.NonPublic | BindingFlags.Static)!;
        const string payload = "{\"metadata\":{},\"status\":\"Failure\",\"reason\":\"NonZeroExitCode\"}";

        var result = (string)method.Invoke(null, [payload])!;

        Assert.That(result, Is.EqualTo(payload));
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
}
