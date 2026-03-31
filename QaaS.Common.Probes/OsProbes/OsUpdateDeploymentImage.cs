using Microsoft.Extensions.Configuration;
using k8s;
using k8s.Models;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.Extensions;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;

namespace QaaS.Common.Probes.OsProbes;

/// <summary>
/// Updates the image of one container in a Kubernetes or OpenShift deployment.
/// </summary>
/// <qaas-docs group="Cluster orchestration" subgroup="Image rollout" />
public class OsUpdateDeploymentImage
    : BaseOsUpdateDeploymentWithGlobalDict<OsUpdateImageProbeConfig>
{
    protected override IEnumerable<ProbeGlobalDictReadRequest> GetAdditionalGlobalDictionaryReadRequests(
        IConfiguration localConfiguration)
    {
        var replicaSetName = localConfiguration[nameof(OsUpdateImageProbeConfig.ReplicaSetName)];
        var containerName = localConfiguration[nameof(OsUpdateImageProbeConfig.ContainerName)];
        if (!string.IsNullOrWhiteSpace(replicaSetName) && !string.IsNullOrWhiteSpace(containerName))
        {
            yield return new ProbeGlobalDictReadRequest("recovery",
                BuildGlobalDictionaryAliasPath("Os", "Recovery", "Image", "Deployment", replicaSetName,
                    containerName));
        }
    }

    protected override V1Deployment UpdateReplicaSet(V1Deployment replicaSet)
    {
        replicaSet.Spec.Template =
            replicaSet.Spec.Template.UpdateReplicaSetImage(Configuration.ContainerName!,
                Configuration.ReplicaSetName!, Configuration.DesiredImage!);

        return Kubernetes.ReplaceNamespacedDeployment(replicaSet, Configuration.ReplicaSetName,
            Configuration.Openshift!.Namespace);
    }

    protected override object? BuildRecoveryConfigurationPatch(V1Deployment replicaSet)
    {
        var container = replicaSet.Spec.Template.Spec.Containers
            .SingleOrDefault(existingContainer => existingContainer.Name == Configuration.ContainerName);
        return container == null
            ? null
            : new
            {
                ContainerName = container.Name,
                DesiredImage = container.Image
            };
    }

    protected override IReadOnlyList<string> GetRecoveryAliasPath()
        => BuildGlobalDictionaryAliasPath("Os", "Recovery", "Image", "Deployment", Configuration.ReplicaSetName!,
            Configuration.ContainerName!);
}
