using k8s;
using k8s.Models;
using QaaS.Common.Probes.ConfigurationObjects.Os;

namespace QaaS.Common.Probes.OsProbes;

/// <summary>
/// Probe that scales openshift deployments
/// </summary>
public class OsScaleDeploymentPods
    : BaseOsUpdateDeployment<OsScalePodsProbeConfig>
{
    protected override V1Deployment UpdateReplicaSet(V1Deployment replicaSet)
    {
        replicaSet.Spec.Replicas = Configuration.DesiredNumberOfPods;
        return Kubernetes.ReplaceNamespacedDeployment(replicaSet, Configuration.ReplicaSetName,
            Configuration.Openshift!.Namespace);
    }
}