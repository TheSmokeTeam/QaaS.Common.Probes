using k8s;
using k8s.Models;
using QaaS.Common.Probes.ConfigurationObjects.Os;

namespace QaaS.Common.Probes.OsProbes;

/// <summary>
/// Probe that scales openshift stateful sets
/// </summary>
/// <qaas-docs group="Cluster orchestration" subgroup="Scaling" />
public class OsScaleStatefulSetPods : BaseOsUpdateStatefulSet<OsScalePodsProbeConfig>
{
    protected override V1StatefulSet UpdateReplicaSet(V1StatefulSet replicaSet)
    {
        replicaSet.Spec.Replicas = Configuration.DesiredNumberOfPods;
        return Kubernetes.ReplaceNamespacedStatefulSet(replicaSet, Configuration.ReplicaSetName,
            Configuration.Openshift!.Namespace);
    }
}
