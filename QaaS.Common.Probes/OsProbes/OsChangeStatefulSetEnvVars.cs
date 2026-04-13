using Microsoft.Extensions.Configuration;
using k8s;
using k8s.Models;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.Extensions;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;

namespace QaaS.Common.Probes.OsProbes;

/// <summary>
/// Probe that changes the environment variables of a statefulSet
/// </summary>
/// <qaas-docs group="Cluster orchestration" subgroup="Environment variables" />
public class OsChangeStatefulSetEnvVars :
    BaseOsUpdateStatefulSetWithGlobalDict<OsChangeEnvVarsConfig>
{
    protected override IEnumerable<ProbeGlobalDictReadRequest> GetAdditionalGlobalDictionaryReadRequests(
        IConfiguration localConfiguration)
    {
        var replicaSetName = localConfiguration[nameof(OsChangeEnvVarsConfig.ReplicaSetName)];
        if (!string.IsNullOrWhiteSpace(replicaSetName))
        {
            yield return new ProbeGlobalDictReadRequest("recovery",
                BuildGlobalDictionaryAliasPath("Os", "Recovery", "EnvVars", "StatefulSet", replicaSetName,
                    localConfiguration[nameof(OsChangeEnvVarsConfig.ContainerName)] ?? "__all__"));
        }
    }

    protected override V1StatefulSet UpdateReplicaSet(V1StatefulSet replicaSet)
    {
        if (Configuration.ContainerEnvVarsToUpdate is { Count: > 0 })
        {
            ReplicaSetUpdateExtensions.RestoreReplicaSetEnvVars(replicaSet.Spec.Template.Spec.Containers,
                Configuration.ContainerEnvVarsToUpdate);
        }
        else
        {
            ReplicaSetUpdateExtensions.ChangeReplicaSetEnvVars(
                replicaSet.Spec.Template.Spec.Containers, Configuration.EnvVarsToUpdate, Configuration.EnvVarsToRemove,
                Configuration.ContainerName);
        }
        replicaSet.Spec.Template.TouchReplicaSetTemplate();

        return Kubernetes.ReplaceNamespacedStatefulSet(replicaSet, Configuration.ReplicaSetName,
            Configuration.Openshift!.Namespace);
    }

    protected override object? BuildRecoveryConfigurationPatch(V1StatefulSet replicaSet)
    {
        return BuildRecoveryConfigurationPatch(replicaSet.Spec.Template.Spec.Containers, Configuration.ContainerName);
    }

    protected override IReadOnlyList<string> GetRecoveryAliasPath()
        => BuildGlobalDictionaryAliasPath("Os", "Recovery", "EnvVars", "StatefulSet", Configuration.ReplicaSetName!,
            Configuration.ContainerName ?? "__all__");

    private static object? BuildRecoveryConfigurationPatch(IList<V1Container> containers, string? containerName)
    {
        var targetContainers = string.IsNullOrWhiteSpace(containerName)
            ? containers.ToArray()
            : containers.Where(container => container.Name == containerName).ToArray();
        if (targetContainers.Length == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(containerName) && targetContainers.Length > 1)
        {
            return new
            {
                ContainerEnvVarsToUpdate = targetContainers.ToDictionary(container => container.Name,
                    container => (container.Env ?? [])
                        .ToDictionary(environmentVariable => environmentVariable.Name,
                            environmentVariable => environmentVariable.Value)),
                EnvVarsToRemove = Array.Empty<string>()
            };
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
