using k8s;
using k8s.Models;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.Extensions;

namespace QaaS.Common.Probes.OsProbes;

/// <summary>
/// Updates container resource requests and limits in a Kubernetes or OpenShift stateful set.
/// </summary>
public class OsUpdateStatefulSetResources : BaseOsUpdateStatefulSet<OsUpdateResourcesProbeConfig>
{
    protected override V1StatefulSet UpdateReplicaSet(V1StatefulSet replicaSet)
    {
        replicaSet.Spec.Template =
            replicaSet.Spec.Template.UpdateReplicaSetResources(Configuration.ContainerName!,
                Configuration.ReplicaSetName!, Configuration.DesiredResources);

        return Kubernetes.ReplaceNamespacedStatefulSet(replicaSet, Configuration.ReplicaSetName,
            Configuration.Openshift!.Namespace);
    }
}
