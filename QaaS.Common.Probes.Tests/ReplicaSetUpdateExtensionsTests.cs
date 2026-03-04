using k8s.Models;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.Extensions;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class ReplicaSetUpdateExtensionsTests
{
    [Test]
    public void TestUpdateReplicaSetResources_ShouldOverrideConfiguredValuesAndKeepExisting()
    {
        // Arrange
        const string replicaSetName = "replica-set";
        const string containerName = "app";
        var template = CreateTemplate(containerName);
        var desiredResources = new Resources
        {
            Limits = new ResourceUnit { Cpu = "200m" },
            Requests = new ResourceUnit { Memory = "64Mi" }
        };

        // Act
        template.UpdateReplicaSetResources(containerName, replicaSetName, desiredResources);
        var resources = template.Spec.Containers.Single(c => c.Name == containerName).Resources;

        // Assert
        Assert.That(resources.Limits!["cpu"].ToString(), Is.EqualTo("200m"));
        Assert.That(resources.Limits!["memory"].ToString(), Is.EqualTo("256Mi"));
        Assert.That(resources.Requests!["cpu"].ToString(), Is.EqualTo("100m"));
        Assert.That(resources.Requests!["memory"].ToString(), Is.EqualTo("64Mi"));
    }

    [Test]
    public void TestUpdateReplicaSetResources_WhenContainerDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var template = CreateTemplate("app");

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() =>
            template.UpdateReplicaSetResources("missing", "replica-set", new Resources()));
    }

    [Test]
    public void TestUpdateReplicaSetImage_ShouldUpdateContainerImage()
    {
        // Arrange
        var template = CreateTemplate("app");

        // Act
        template.UpdateReplicaSetImage("app", "replica-set", "new-image:v2");

        // Assert
        Assert.That(template.Spec.Containers.Single(c => c.Name == "app").Image, Is.EqualTo("new-image:v2"));
    }

    [Test]
    public void TestChangeReplicaSetEnvVars_WithSpecificContainer_ShouldOnlyAffectThatContainer()
    {
        // Arrange
        var containers = new List<V1Container>
        {
            new()
            {
                Name = "app",
                Env =
                [
                    new V1EnvVar { Name = "EXISTING", Value = "1" },
                    new V1EnvVar { Name = "TO_REMOVE", Value = "x" }
                ]
            },
            new()
            {
                Name = "sidecar",
                Env =
                [
                    new V1EnvVar { Name = "EXISTING", Value = "2" }
                ]
            }
        };

        // Act
        ReplicaSetUpdateExtensions.ChangeReplicaSetEnvVars(
            containers,
            new Dictionary<string, string?> { ["EXISTING"] = "10", ["NEW"] = "abc" },
            ["TO_REMOVE"],
            "app");

        // Assert
        var appEnv = containers[0].Env!.ToDictionary(e => e.Name, e => e.Value);
        var sidecarEnv = containers[1].Env!.ToDictionary(e => e.Name, e => e.Value);

        Assert.That(appEnv["EXISTING"], Is.EqualTo("10"));
        Assert.That(appEnv["NEW"], Is.EqualTo("abc"));
        Assert.That(appEnv.ContainsKey("TO_REMOVE"), Is.False);
        Assert.That(sidecarEnv["EXISTING"], Is.EqualTo("2"));
        Assert.That(sidecarEnv.ContainsKey("NEW"), Is.False);
    }

    [Test]
    public void TestChangeReplicaSetEnvVars_WhenEnvVarToRemoveMissing_ShouldThrowArgumentException()
    {
        // Arrange
        var containers = new List<V1Container>
        {
            new()
            {
                Name = "app",
                Env = [new V1EnvVar { Name = "EXISTING", Value = "1" }]
            }
        };

        // Act + Assert
        Assert.Throws<ArgumentException>(() => ReplicaSetUpdateExtensions.ChangeReplicaSetEnvVars(
            containers,
            new Dictionary<string, string?>(),
            ["MISSING"],
            null));
    }

    [Test]
    public void TestChangeReplicaSetEnvVars_WhenSpecifiedContainerIsMissing_ShouldThrowArgumentException()
    {
        // Arrange
        var containers = new List<V1Container>
        {
            new()
            {
                Name = "app",
                Env = [new V1EnvVar { Name = "EXISTING", Value = "1" }]
            }
        };

        // Act + Assert
        Assert.Throws<ArgumentException>(() => ReplicaSetUpdateExtensions.ChangeReplicaSetEnvVars(
            containers,
            new Dictionary<string, string?> { ["EXISTING"] = "2" },
            [],
            "missing-container"));
    }

    private static V1PodTemplateSpec CreateTemplate(string containerName)
    {
        return new V1PodTemplateSpec
        {
            Spec = new V1PodSpec
            {
                Containers =
                [
                    new V1Container
                    {
                        Name = containerName,
                        Image = "old-image:v1",
                        Resources = new V1ResourceRequirements
                        {
                            Limits = new Dictionary<string, ResourceQuantity>
                            {
                                ["cpu"] = new ResourceQuantity("100m"),
                                ["memory"] = new ResourceQuantity("256Mi")
                            },
                            Requests = new Dictionary<string, ResourceQuantity>
                            {
                                ["cpu"] = new ResourceQuantity("100m"),
                                ["memory"] = new ResourceQuantity("128Mi")
                            }
                        }
                    }
                ]
            }
        };
    }
}
