using k8s;
using k8s.Models;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.Extensions;

namespace QaaS.Common.Probes.OsProbes;

/// <summary>
/// Probe that changes the environment variables of a statefulSet
/// </summary>
/// <qaas-docs group="Cluster orchestration" subgroup="Environment variables" />
public class OsChangeStatefulSetEnvVars :
    BaseOsUpdateStatefulSet<OsChangeEnvVarsConfig>
{
    /// <inheritdoc />
    protected override V1StatefulSet UpdateReplicaSet(V1StatefulSet replicaSet)
    {
        ReplicaSetUpdateExtensions.ChangeReplicaSetEnvVars(
            replicaSet.Spec.Template.Spec.Containers, Configuration.EnvVarsToUpdate, Configuration.EnvVarsToRemove,
            Configuration.ContainerName);
        replicaSet.Spec.Template.TouchReplicaSetTemplate();

        return Kubernetes.ReplaceNamespacedStatefulSet(replicaSet, Configuration.ReplicaSetName,
            Configuration.Openshift!.Namespace);
    }
}
