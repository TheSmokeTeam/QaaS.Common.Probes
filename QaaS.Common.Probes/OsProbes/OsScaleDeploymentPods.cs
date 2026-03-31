using Microsoft.Extensions.Configuration;
using k8s;
using k8s.Models;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;

namespace QaaS.Common.Probes.OsProbes;

/// <summary>
/// Probe that scales openshift deployments
/// </summary>
/// <qaas-docs group="Cluster orchestration" subgroup="Scaling" />
public class OsScaleDeploymentPods : BaseOsUpdateDeploymentWithGlobalDictDefaults<OsScalePodsProbeConfig>
{
    protected override IEnumerable<ProbeGlobalDictReadRequest> GetAdditionalGlobalDictionaryReadRequests(
        IConfiguration localConfiguration)
    {
        var replicaSetName = localConfiguration[nameof(OsScalePodsProbeConfig.ReplicaSetName)];
        if (!string.IsNullOrWhiteSpace(replicaSetName))
        {
            yield return new ProbeGlobalDictReadRequest("recovery",
                BuildGlobalDictionaryAliasPath("Os", "Recovery", "Scale", "Deployment", replicaSetName));
        }
    }

    protected override V1Deployment UpdateReplicaSet(V1Deployment replicaSet)
    {
        replicaSet.Spec.Replicas = Configuration.DesiredNumberOfPods;
        return Kubernetes.ReplaceNamespacedDeployment(replicaSet, Configuration.ReplicaSetName,
            Configuration.Openshift!.Namespace);
    }

    protected override object BuildRecoveryConfigurationPatch(V1Deployment replicaSet)
        => new { DesiredNumberOfPods = (int)(replicaSet.Spec.Replicas ?? 0) };

    protected override IReadOnlyList<string> GetRecoveryAliasPath()
        => BuildGlobalDictionaryAliasPath("Os", "Recovery", "Scale", "Deployment", Configuration.ReplicaSetName!);
}
