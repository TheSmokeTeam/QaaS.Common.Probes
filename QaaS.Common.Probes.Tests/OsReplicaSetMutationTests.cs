using k8s;
using k8s.Models;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.OsProbes;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class OsReplicaSetMutationTests
{
    private sealed class TestableBaseOsUpdateDeployment : BaseOsUpdateDeployment<OsUpdatePodsProbeConfig>
    {
        public void SetClient(Kubernetes kubernetesClient) => Kubernetes = kubernetesClient;

        public V1Deployment InvokeReadReplicaSet() => ReadReplicaSet();

        public bool InvokeIsReplicaSetAvailable(V1Deployment replicaSet) => IsReplicaSetAvailable(replicaSet);

        protected override V1Deployment UpdateReplicaSet(V1Deployment replicaSet) => replicaSet;
    }

    private sealed class TestableBaseOsUpdateStatefulSet : BaseOsUpdateStatefulSet<OsUpdatePodsProbeConfig>
    {
        public void SetClient(Kubernetes kubernetesClient) => Kubernetes = kubernetesClient;

        public V1StatefulSet InvokeReadReplicaSet() => ReadReplicaSet();

        public bool InvokeIsReplicaSetAvailable(V1StatefulSet replicaSet) => IsReplicaSetAvailable(replicaSet);

        protected override V1StatefulSet UpdateReplicaSet(V1StatefulSet replicaSet) => replicaSet;
    }

    private sealed class TestableOsChangeDeploymentEnvVars : OsChangeDeploymentEnvVars
    {
        public void SetClient(Kubernetes kubernetesClient) => Kubernetes = kubernetesClient;

        public V1Deployment InvokeUpdateReplicaSet(V1Deployment replicaSet) => UpdateReplicaSet(replicaSet);
    }

    private sealed class TestableOsUpdateDeploymentImage : OsUpdateDeploymentImage
    {
        public void SetClient(Kubernetes kubernetesClient) => Kubernetes = kubernetesClient;

        public V1Deployment InvokeUpdateReplicaSet(V1Deployment replicaSet) => UpdateReplicaSet(replicaSet);
    }

    [Test]
    public void BaseOsUpdateDeployment_ShouldReadReplicaSetAndReturnFalseWhenReplicaCountsMismatch()
    {
        using var server = new TestHttpServer();
        server.EnqueueJsonResponse(
            "GET",
            "/apis/apps/v1/namespaces/namespace/deployments/replica-set",
            """
            {
              "apiVersion": "apps/v1",
              "kind": "Deployment",
              "metadata": { "name": "replica-set", "generation": 3 },
              "spec": { "replicas": 3 },
              "status": {
                "observedGeneration": 3,
                "replicas": 3,
                "availableReplicas": 2,
                "updatedReplicas": 3,
                "readyReplicas": 3
              }
            }
            """);

        var probe = new TestableBaseOsUpdateDeployment
        {
            Configuration = CreateUpdatePodsConfig(),
            Context = Globals.Context
        };
        probe.SetClient(server.CreateKubernetesClient());

        var deployment = probe.InvokeReadReplicaSet();

        Assert.That(deployment.Metadata!.Name, Is.EqualTo("replica-set"));
        Assert.That(probe.InvokeIsReplicaSetAvailable(deployment), Is.False);
    }

    [Test]
    public void BaseOsUpdateStatefulSet_ShouldReadReplicaSetAndReturnFalseWhenUpdatedReplicasMismatch()
    {
        using var server = new TestHttpServer();
        server.EnqueueJsonResponse(
            "GET",
            "/apis/apps/v1/namespaces/namespace/statefulsets/replica-set",
            """
            {
              "apiVersion": "apps/v1",
              "kind": "StatefulSet",
              "metadata": { "name": "replica-set", "generation": 4 },
              "spec": { "replicas": 3 },
              "status": {
                "observedGeneration": 4,
                "replicas": 3,
                "readyReplicas": 3,
                "updatedReplicas": 2
              }
            }
            """);

        var probe = new TestableBaseOsUpdateStatefulSet
        {
            Configuration = CreateUpdatePodsConfig(),
            Context = Globals.Context
        };
        probe.SetClient(server.CreateKubernetesClient());

        var statefulSet = probe.InvokeReadReplicaSet();

        Assert.That(statefulSet.Metadata!.Name, Is.EqualTo("replica-set"));
        Assert.That(probe.InvokeIsReplicaSetAvailable(statefulSet), Is.False);
    }

    [Test]
    public void OsChangeDeploymentEnvVars_ShouldReplaceDeploymentWithUpdatedEnvironmentAndMutationAnnotation()
    {
        using var server = new TestHttpServer();
        server.EnqueueJsonResponse(
            "PUT",
            "/apis/apps/v1/namespaces/namespace/deployments/replica-set",
            """
            {
              "apiVersion": "apps/v1",
              "kind": "Deployment",
              "metadata": { "name": "replica-set" },
              "spec": {
                "template": {
                  "metadata": {
                    "annotations": {
                      "qaas.smoketeam.io/last-mutation-id": "mutation"
                    }
                  },
                  "spec": {
                    "containers": [
                      {
                        "name": "app",
                        "env": [
                          { "name": "EXISTING", "value": "updated" },
                          { "name": "NEW", "value": "value" }
                        ]
                      }
                    ]
                  }
                }
              }
            }
            """,
            assertBody: body =>
            {
                Assert.That(body, Does.Contain("\"EXISTING\",\"value\":\"updated\""));
                Assert.That(body, Does.Contain("\"NEW\",\"value\":\"value\""));
                Assert.That(body, Does.Contain("qaas.smoketeam.io/last-mutation-id"));
            });

        var probe = new TestableOsChangeDeploymentEnvVars
        {
            Configuration = new OsChangeEnvVarsConfig
            {
                Openshift = CreateOpenshiftConfig(),
                ReplicaSetName = "replica-set",
                ContainerName = "app",
                EnvVarsToUpdate = new Dictionary<string, string?> { ["EXISTING"] = "updated", ["NEW"] = "value" },
                EnvVarsToRemove = ["REMOVE_ME"]
            },
            Context = Globals.Context
        };
        probe.SetClient(server.CreateKubernetesClient());

        var deployment = probe.InvokeUpdateReplicaSet(CreateDeploymentWithContainer());
        var environment = deployment.Spec.Template.Spec.Containers.Single().Env!.ToDictionary(env => env.Name, env => env.Value);

        Assert.That(environment["EXISTING"], Is.EqualTo("updated"));
        Assert.That(environment["NEW"], Is.EqualTo("value"));
        Assert.That(deployment.Spec.Template.Metadata!.Annotations,
            Contains.Key("qaas.smoketeam.io/last-mutation-id"));
    }

    [Test]
    public void OsUpdateDeploymentImage_ShouldReplaceDeploymentWithUpdatedImage()
    {
        using var server = new TestHttpServer();
        server.EnqueueJsonResponse(
            "PUT",
            "/apis/apps/v1/namespaces/namespace/deployments/replica-set",
            """
            {
              "apiVersion": "apps/v1",
              "kind": "Deployment",
              "metadata": { "name": "replica-set" },
              "spec": {
                "template": {
                  "spec": {
                    "containers": [
                      {
                        "name": "app",
                        "image": "new-image:v2"
                      }
                    ]
                  }
                }
              }
            }
            """,
            assertBody: body => Assert.That(body, Does.Contain("\"image\":\"new-image:v2\"")));

        var probe = new TestableOsUpdateDeploymentImage
        {
            Configuration = new OsUpdateImageProbeConfig
            {
                Openshift = CreateOpenshiftConfig(),
                ReplicaSetName = "replica-set",
                ContainerName = "app",
                DesiredImage = "new-image:v2"
            },
            Context = Globals.Context
        };
        probe.SetClient(server.CreateKubernetesClient());

        var deployment = probe.InvokeUpdateReplicaSet(CreateDeploymentWithContainer());

        Assert.That(deployment.Spec.Template.Spec.Containers.Single().Image, Is.EqualTo("new-image:v2"));
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

    private static OsUpdatePodsProbeConfig CreateUpdatePodsConfig()
    {
        return new OsUpdatePodsProbeConfig
        {
            Openshift = CreateOpenshiftConfig(),
            ReplicaSetName = "replica-set"
        };
    }

    private static V1Deployment CreateDeploymentWithContainer()
    {
        return new V1Deployment
        {
            Spec = new V1DeploymentSpec
            {
                Template = new V1PodTemplateSpec
                {
                    Spec = new V1PodSpec
                    {
                        Containers =
                        [
                            new V1Container
                            {
                                Name = "app",
                                Image = "old-image:v1",
                                Env =
                                [
                                    new V1EnvVar { Name = "EXISTING", Value = "old" },
                                    new V1EnvVar { Name = "REMOVE_ME", Value = "x" }
                                ]
                            }
                        ]
                    }
                }
            }
        };
    }
}
