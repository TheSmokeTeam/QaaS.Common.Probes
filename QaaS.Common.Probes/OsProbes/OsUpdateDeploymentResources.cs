using k8s;
using k8s.Models;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.Extensions;

namespace QaaS.Common.Probes.OsProbes;

/// <summary>
/// Updates container resource requests and limits in a Kubernetes or OpenShift deployment.
/// </summary>
/// <qaas-docs group="Cluster orchestration" subgroup="Resource tuning" />
public class OsUpdateDeploymentResources
    : BaseOsUpdateDeployment<OsUpdateResourcesProbeConfig>
{
    protected override V1Deployment UpdateReplicaSet(V1Deployment replicaSet)
    {
        replicaSet.Spec.Template =
            replicaSet.Spec.Template.UpdateReplicaSetResources(Configuration.ContainerName!,
                Configuration.ReplicaSetName!, Configuration.DesiredResources);

        return Kubernetes.ReplaceNamespacedDeployment(replicaSet, Configuration.ReplicaSetName,
            Configuration.Openshift!.Namespace);
    }
}
