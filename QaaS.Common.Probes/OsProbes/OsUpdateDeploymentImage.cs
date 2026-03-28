using k8s;
using k8s.Models;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.Extensions;

namespace QaaS.Common.Probes.OsProbes;

/// <summary>
/// Updates the image of one container in a Kubernetes or OpenShift deployment.
/// </summary>
/// <qaas-docs group="Cluster orchestration" subgroup="Image rollout" />
public class OsUpdateDeploymentImage
    : BaseOsUpdateDeployment<OsUpdateImageProbeConfig>
{
    protected override V1Deployment UpdateReplicaSet(V1Deployment replicaSet)
    {
        replicaSet.Spec.Template =
            replicaSet.Spec.Template.UpdateReplicaSetImage(Configuration.ContainerName!,
                Configuration.ReplicaSetName!, Configuration.DesiredImage!);

        return Kubernetes.ReplaceNamespacedDeployment(replicaSet, Configuration.ReplicaSetName,
            Configuration.Openshift!.Namespace);
    }
}
