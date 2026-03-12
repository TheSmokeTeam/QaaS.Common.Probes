using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.Os;

namespace QaaS.Common.Probes.OsProbes;

public abstract class
    BaseOsUpdateStatefulSet<TOsUpdatePodsProbeConfig>
    : BaseOsUpdatePodsProbe<TOsUpdatePodsProbeConfig, V1StatefulSet>
    where TOsUpdatePodsProbeConfig : OsUpdatePodsProbeConfig, new()
{
    protected override bool IsReplicaSetAvailable(V1StatefulSet replicaSet)
    {
        Context.Logger.LogDebug(
            "Checking StatefulSet {StatefulSetName} availability, currently it has " +
            "{NumberOfReadyPodsInStatefulSet} ready pods" +
            " and {NumberOfReplicasInStatus} pods in status",
            Configuration.ReplicaSetName,
            replicaSet.Status.ReadyReplicas ?? 0,
            replicaSet.Status.Replicas);

        return (replicaSet.Status.ReadyReplicas ?? 0) == replicaSet.Status.Replicas &&
               (replicaSet.Spec.Replicas ?? 0) == replicaSet.Status.Replicas &&
               (replicaSet.Status.ReadyReplicas ?? 0) == replicaSet.Status.Replicas &&
               (replicaSet.Status.UpdatedReplicas ?? 0) == (replicaSet.Spec.Replicas ?? 0);
    }

    protected override long? GetReplicaSetGeneration(V1StatefulSet replicaSet) => replicaSet.Metadata?.Generation;

    protected override long? GetObservedGeneration(V1StatefulSet replicaSet) => replicaSet.Status?.ObservedGeneration;

    protected override V1StatefulSet ReadReplicaSet()
    {
        return Kubernetes.ReadNamespacedStatefulSet(Configuration.ReplicaSetName,
            Configuration.Openshift!.Namespace);
    }
}
