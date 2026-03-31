using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.Os;

namespace QaaS.Common.Probes.OsProbes;

/// <summary>
/// Shared OpenShift mutation probe base that supports probe-global-dictionary defaults and optional recovery payloads.
/// Derived probes update one replica-set-like resource, wait for convergence, and then publish a sparse recovery patch
/// that a later rollback probe can consume when local configuration omits the restore values.
/// </summary>
public abstract class BaseOsUpdatePodsProbeWithGlobalDict<TOsUpdatePodsProbeConfig, TReplicaSet>
    : BaseOsProbeWithGlobalDict<TOsUpdatePodsProbeConfig>
    where TOsUpdatePodsProbeConfig : OsUpdatePodsProbeConfig, new()
{
    protected override void RunOsProbe()
    {
        Context.Logger.LogInformation("Running probe to update {ReplicaSetName} ReplicaSet",
            Configuration.ReplicaSetName);
        var replicaSet = ReadReplicaSet();
        var recoveryConfigurationPatch = BuildRecoveryConfigurationPatch(replicaSet);

        replicaSet = UpdateReplicaSet(replicaSet);
        var desiredGeneration = GetReplicaSetGeneration(replicaSet);
        Context.Logger.LogInformation("Updated ReplicaSet, waiting for ReplicaSet to reach desired state");
        WaitForReplicaCountToReachDesiredState(replicaSet,
            Configuration.IntervalBetweenDesiredStateChecksMs,
            Configuration.TimeoutWaitForDesiredStateSeconds,
            desiredGeneration);

        // Recovery payloads are written only after the mutation converges so later probes never restore a half-applied state.
        // The alias path is computed lazily so legacy direct Run() tests keep working when global-dictionary support is disabled.
        if (Configuration.UseGlobalDict && recoveryConfigurationPatch != null)
        {
            var recoveryAliasPath = GetRecoveryAliasPath();
            if (recoveryAliasPath.Count != 0)
            {
                SaveGlobalDictionaryPayload("recovery", recoveryConfigurationPatch, recoveryAliasPath);
            }
        }
    }

    private void WaitForReplicaCountToReachDesiredState(TReplicaSet replicaSet,
        int milliSecondsTimeOutBetweenChecks,
        int maximumTimeOutSeconds,
        long? desiredGeneration)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        Thread.Sleep(milliSecondsTimeOutBetweenChecks);
        replicaSet = ReadReplicaSet();

        while (!HasReplicaSetReachedDesiredState(replicaSet, desiredGeneration))
        {
            if (stopWatch.ElapsedMilliseconds >= maximumTimeOutSeconds * 1000)
            {
                Context.Logger.LogError(
                    "ReplicaSet {ReplicaSetName} has not finished updating before timeout of" +
                    " {SecondsTimeout} seconds was reached",
                    Configuration.ReplicaSetName, maximumTimeOutSeconds);
                return;
            }

            Thread.Sleep(milliSecondsTimeOutBetweenChecks);

            replicaSet = ReadReplicaSet();
        }

        Context.Logger.LogInformation("Finished updating ReplicaSet {ReplicaSetName} in " +
                                      "{NumberOfMillisecondsItTookToScale} milliseconds successfully",
            Configuration.ReplicaSetName, stopWatch.ElapsedMilliseconds);
    }

    private bool HasReplicaSetReachedDesiredState(TReplicaSet replicaSet, long? desiredGeneration)
    {
        var observedGeneration = GetObservedGeneration(replicaSet);
        return (desiredGeneration == null || observedGeneration == null || observedGeneration >= desiredGeneration) &&
               IsReplicaSetAvailable(replicaSet);
    }

    protected abstract bool IsReplicaSetAvailable(TReplicaSet replicaSet);

    protected abstract long? GetReplicaSetGeneration(TReplicaSet replicaSet);

    protected abstract long? GetObservedGeneration(TReplicaSet replicaSet);

    protected abstract TReplicaSet ReadReplicaSet();

    protected abstract TReplicaSet UpdateReplicaSet(TReplicaSet replicaSet);

    /// <summary>
    /// Builds the sparse configuration patch that can later restore the current replica set.
    /// </summary>
    protected virtual object? BuildRecoveryConfigurationPatch(TReplicaSet replicaSet) => null;

    /// <summary>
    /// Returns the alias path used by related recovery probes to resolve the saved pre-mutation state.
    /// </summary>
    protected virtual IReadOnlyList<string> GetRecoveryAliasPath() => [];
}
