using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.Os;

namespace QaaS.Common.Probes.OsProbes;

public abstract class
    BaseOsUpdateDeployment<TOsUpdatePodsProbeConfig>
    : BaseOsUpdatePodsProbe<TOsUpdatePodsProbeConfig, V1Deployment>
    where TOsUpdatePodsProbeConfig : OsUpdatePodsProbeConfig, new()
{
    protected override bool IsReplicaSetAvailable(V1Deployment replicaSet)
    {
        Context.Logger.LogDebug(
            "Checking Deployment {DeploymentName} availability, currently it has " +
            "{NumberOfUnavailablePodsInDeployment} unavailable pods" +
            ", {NumberOfAvailablePodsInDeployment} available pods" +
            " and {NumberOfReplicasInStatus} pods in status",
            Configuration.ReplicaSetName,
            replicaSet.Status.UnavailableReplicas ?? 0,
            replicaSet.Status.AvailableReplicas ?? 0,
            replicaSet.Status.Replicas ?? 0);

        return replicaSet.Status.UnavailableReplicas == null &&
               (replicaSet.Status.AvailableReplicas ?? 0) == (replicaSet.Status.Replicas ?? 0) &&
               (replicaSet.Spec.Replicas ?? 0) == (replicaSet.Status.Replicas ?? 0) &&
               (replicaSet.Status.UpdatedReplicas ?? 0) == (replicaSet.Spec.Replicas ?? 0) &&
               (replicaSet.Status.ReadyReplicas ?? 0) == (replicaSet.Status.Replicas ?? 0);
    }

    protected override V1Deployment ReadReplicaSet()
    {
        return Kubernetes.ReadNamespacedDeployment(Configuration.ReplicaSetName,
            Configuration.Openshift!.Namespace);
    }
}