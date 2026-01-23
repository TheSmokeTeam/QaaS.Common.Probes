using k8s;
using k8s.Models;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.Extensions;

namespace QaaS.Common.Probes.OsProbes;

/// <summary>
/// Probe that changes the environment variables of a deployment
/// </summary>
public class OsChangeDeploymentEnvVars :
    BaseOsUpdateDeployment<OsChangeEnvVarsConfig>
{
    /// <inheritdoc />
    protected override V1Deployment UpdateReplicaSet(V1Deployment replicaSet)
    {
        ReplicaSetUpdateExtensions.ChangeReplicaSetEnvVars(
            replicaSet.Spec.Template.Spec.Containers, Configuration.EnvVarsToUpdate, Configuration.EnvVarsToRemove,
            Configuration.ContainerName);

        return Kubernetes.ReplaceNamespacedDeployment(replicaSet, Configuration.ReplicaSetName,
            Configuration.Openshift!.Namespace);
    }
}