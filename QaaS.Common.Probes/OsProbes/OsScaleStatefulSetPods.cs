using Microsoft.Extensions.Configuration;
using k8s;
using k8s.Models;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;

namespace QaaS.Common.Probes.OsProbes;

/// <summary>
/// Probe that scales openshift statefulsets
/// </summary>
/// <qaas-docs group="Cluster orchestration" subgroup="Scaling" />
public class OsScaleStatefulSetPods : BaseOsUpdateStatefulSetWithGlobalDict<OsScalePodsProbeConfig>
{
    protected override IEnumerable<ProbeGlobalDictReadRequest> GetAdditionalGlobalDictionaryReadRequests(
        IConfiguration localConfiguration)
    {
        var replicaSetName = localConfiguration[nameof(OsScalePodsProbeConfig.ReplicaSetName)];
        if (!string.IsNullOrWhiteSpace(replicaSetName))
        {
            yield return new ProbeGlobalDictReadRequest("recovery",
                BuildGlobalDictionaryAliasPath("Os", "Recovery", "Scale", "StatefulSet", replicaSetName));
        }
    }

    protected override V1StatefulSet UpdateReplicaSet(V1StatefulSet replicaSet)
    {
        replicaSet.Spec.Replicas = Configuration.DesiredNumberOfPods;
        return Kubernetes.ReplaceNamespacedStatefulSet(replicaSet, Configuration.ReplicaSetName,
            Configuration.Openshift!.Namespace);
    }

    protected override object BuildRecoveryConfigurationPatch(V1StatefulSet replicaSet)
        => new { DesiredNumberOfPods = (int)(replicaSet.Spec.Replicas ?? 0) };

    protected override IReadOnlyList<string> GetRecoveryAliasPath()
        => BuildGlobalDictionaryAliasPath("Os", "Recovery", "Scale", "StatefulSet", Configuration.ReplicaSetName!);
}
