using Microsoft.Extensions.Configuration;
using k8s;
using k8s.Models;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.Extensions;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;

namespace QaaS.Common.Probes.OsProbes;

/// <summary>
/// Updates container resource requests and limits in a Kubernetes or OpenShift stateful set.
/// </summary>
/// <qaas-docs group="Cluster orchestration" subgroup="Resource tuning" />
public class OsUpdateStatefulSetResources
    : BaseOsUpdateStatefulSetWithGlobalDictDefaults<OsUpdateResourcesProbeConfig>
{
    protected override IEnumerable<ProbeGlobalDictReadRequest> GetAdditionalGlobalDictionaryReadRequests(
        IConfiguration localConfiguration)
    {
        var replicaSetName = localConfiguration[nameof(OsUpdateResourcesProbeConfig.ReplicaSetName)];
        var containerName = localConfiguration[nameof(OsUpdateResourcesProbeConfig.ContainerName)];
        if (!string.IsNullOrWhiteSpace(replicaSetName) && !string.IsNullOrWhiteSpace(containerName))
        {
            yield return new ProbeGlobalDictReadRequest("recovery",
                BuildGlobalDictionaryAliasPath("Os", "Recovery", "Resources", "StatefulSet", replicaSetName,
                    containerName));
        }
    }

    protected override V1StatefulSet UpdateReplicaSet(V1StatefulSet replicaSet)
    {
        replicaSet.Spec.Template =
            replicaSet.Spec.Template.UpdateReplicaSetResources(Configuration.ContainerName!,
                Configuration.ReplicaSetName!, Configuration.DesiredResources);

        return Kubernetes.ReplaceNamespacedStatefulSet(replicaSet, Configuration.ReplicaSetName,
            Configuration.Openshift!.Namespace);
    }

    protected override object? BuildRecoveryConfigurationPatch(V1StatefulSet replicaSet)
    {
        var container = replicaSet.Spec.Template.Spec.Containers
            .SingleOrDefault(existingContainer => existingContainer.Name == Configuration.ContainerName);
        return container == null
            ? null
            : new
            {
                ContainerName = container.Name,
                DesiredResources = ToConfigurationResources(container.Resources)
            };
    }

    protected override IReadOnlyList<string> GetRecoveryAliasPath()
        => BuildGlobalDictionaryAliasPath("Os", "Recovery", "Resources", "StatefulSet", Configuration.ReplicaSetName!,
            Configuration.ContainerName!);

    private static Resources ToConfigurationResources(V1ResourceRequirements? resourceRequirements)
    {
        return new Resources
        {
            Limits = ToResourceUnit(resourceRequirements?.Limits),
            Requests = ToResourceUnit(resourceRequirements?.Requests)
        };
    }

    private static ResourceUnit? ToResourceUnit(IDictionary<string, ResourceQuantity>? resourceDictionary)
    {
        if (resourceDictionary == null || resourceDictionary.Count == 0)
        {
            return null;
        }

        return new ResourceUnit
        {
            Cpu = resourceDictionary.TryGetValue("cpu", out var cpu) ? cpu.ToString() : null,
            Memory = resourceDictionary.TryGetValue("memory", out var memory) ? memory.ToString() : null
        };
    }
}
