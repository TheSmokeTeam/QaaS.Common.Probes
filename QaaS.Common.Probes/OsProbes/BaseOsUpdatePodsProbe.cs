using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.Os;

namespace QaaS.Common.Probes.OsProbes;

public abstract class
    BaseOsUpdatePodsProbe<TOsUpdatePodsProbeConfig, TReplicaSet> : BaseOsProbe<TOsUpdatePodsProbeConfig>
    where TOsUpdatePodsProbeConfig : OsUpdatePodsProbeConfig, new()
{
    protected override void RunOsProbe()
    {
        Context.Logger.LogInformation("Running probe to update {ReplicaSetName} ReplicaSet",
            Configuration.ReplicaSetName);
        var replicaSet = ReadReplicaSet();

        replicaSet = UpdateReplicaSet(replicaSet);
        var desiredGeneration = GetReplicaSetGeneration(replicaSet);
        Context.Logger.LogInformation("Updated ReplicaSet, waiting for ReplicaSet to reach desired state");
        WaitForReplicaCountToReachDesiredState(replicaSet,
            Configuration.IntervalBetweenDesiredStateChecksMs,
            Configuration.TimeoutWaitForDesiredStateSeconds,
            desiredGeneration);
    }

    /// <summary>
    /// Function that waits until the number of replicas of the replicaset is equal to the desired state before ending,
    /// and logs whether or not the replicaset reached the desired state before the given timeout
    /// </summary>
    /// <param name="replicaSet"> The replicaSet to wait for </param>
    /// <param name="milliSecondsTimeOutBetweenChecks"> Time in milliseconds between each check if the
    /// amount of replicas available is equal to the desired number of replicas </param>
    /// <param name="maximumTimeOutSeconds"> The maximum timeout in seconds before the function ends and
    /// raises an error log</param>
    /// <param name="desiredGeneration">The generation that must be observed by the controller before the probe completes</param>
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
            // Reached timeout and still running, return false
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

    /// <summary>
    /// Checks whether all replicas in the replica set are available
    /// </summary>
    /// <param name="replicaSet"> The replicaset to check </param>
    /// <returns> True if all replicas are available, false otherwise </returns>
    protected abstract bool IsReplicaSetAvailable(TReplicaSet replicaSet);

    protected abstract long? GetReplicaSetGeneration(TReplicaSet replicaSet);

    protected abstract long? GetObservedGeneration(TReplicaSet replicaSet);

    /// <summary>
    /// Gets the replicaset manifest from the k8s cluster
    /// </summary>
    protected abstract TReplicaSet ReadReplicaSet();

    protected abstract TReplicaSet UpdateReplicaSet(TReplicaSet replicaSet);
}
