using k8s;
using k8s.Models;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.Extensions;

namespace QaaS.Common.Probes.OsProbes;

public class OsUpdateStatefulSetImage
    : BaseOsUpdateStatefulSet<OsUpdateImageProbeConfig>
{
    protected override V1StatefulSet UpdateReplicaSet(V1StatefulSet replicaSet)
    {
        replicaSet.Spec.Template =
            replicaSet.Spec.Template.UpdateReplicaSetImage(Configuration.ContainerName!,
                Configuration.ReplicaSetName!, Configuration.DesiredImage!);

        return Kubernetes.ReplaceNamespacedStatefulSet(replicaSet, Configuration.ReplicaSetName,
            Configuration.Openshift!.Namespace);
    }
}