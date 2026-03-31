using Microsoft.Extensions.Configuration;
using k8s;
using k8s.Models;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.Extensions;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;

namespace QaaS.Common.Probes.OsProbes;

/// <summary>
/// Probe that changes the environment variables of a deployment
/// </summary>
/// <qaas-docs group="Cluster orchestration" subgroup="Environment variables" />
public class OsChangeDeploymentEnvVars :
    BaseOsUpdateDeploymentWithGlobalDictDefaults<OsChangeEnvVarsConfig>
{
    protected override IEnumerable<ProbeGlobalDictReadRequest> GetAdditionalGlobalDictionaryReadRequests(
        IConfiguration localConfiguration)
    {
        var replicaSetName = localConfiguration[nameof(OsChangeEnvVarsConfig.ReplicaSetName)];
        if (!string.IsNullOrWhiteSpace(replicaSetName))
        {
            yield return new ProbeGlobalDictReadRequest("recovery",
                BuildGlobalDictionaryAliasPath("Os", "Recovery", "EnvVars", "Deployment", replicaSetName,
                    localConfiguration[nameof(OsChangeEnvVarsConfig.ContainerName)] ?? "__all__"));
        }
    }

    protected override V1Deployment UpdateReplicaSet(V1Deployment replicaSet)
    {
        ReplicaSetUpdateExtensions.ChangeReplicaSetEnvVars(
            replicaSet.Spec.Template.Spec.Containers, Configuration.EnvVarsToUpdate, Configuration.EnvVarsToRemove,
            Configuration.ContainerName);
        replicaSet.Spec.Template.TouchReplicaSetTemplate();

        return Kubernetes.ReplaceNamespacedDeployment(replicaSet, Configuration.ReplicaSetName,
            Configuration.Openshift!.Namespace);
    }

    protected override object? BuildRecoveryConfigurationPatch(V1Deployment replicaSet)
    {
        return BuildRecoveryConfigurationPatch(replicaSet.Spec.Template.Spec.Containers, Configuration.ContainerName);
    }

    protected override IReadOnlyList<string> GetRecoveryAliasPath()
        => BuildGlobalDictionaryAliasPath("Os", "Recovery", "EnvVars", "Deployment", Configuration.ReplicaSetName!,
            Configuration.ContainerName ?? "__all__");

    private static object? BuildRecoveryConfigurationPatch(IList<V1Container> containers, string? containerName)
    {
        var targetContainers = string.IsNullOrWhiteSpace(containerName)
            ? containers.ToArray()
            : containers.Where(container => container.Name == containerName).ToArray();
        if (targetContainers.Length != 1)
        {
            return null;
        }

        var targetContainer = targetContainers[0];
        return new
        {
            ContainerName = targetContainer.Name,
            EnvVarsToUpdate = (targetContainer.Env ?? [])
                .ToDictionary(environmentVariable => environmentVariable.Name,
                    environmentVariable => environmentVariable.Value),
            EnvVarsToRemove = Array.Empty<string>()
        };
    }
}
