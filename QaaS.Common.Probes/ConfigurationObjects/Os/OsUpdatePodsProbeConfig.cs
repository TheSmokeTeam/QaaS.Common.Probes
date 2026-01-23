using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Os;

public record OsUpdatePodsProbeConfig : OsProbeConfig
{
    [Required, Description("The name of the replicaset to scale the pods of")]
    public string? ReplicaSetName { get; set; }

    [Range(0, int.MaxValue), Description(
         "The interval in milliseconds between every check of the replica set's state (if it reached the desired number of pods yet)"),
     DefaultValue(1000)]
    public int IntervalBetweenDesiredStateChecksMs { get; set; } = 1000;

    [Range(0, int.MaxValue), Description(
         "The timeout in seconds for waiting for the replicaset to scale to the given number of pods, when timeout" +
         " is reached an error log is raised and the code continues to run"),
     DefaultValue(300)]
    public int TimeoutWaitForDesiredStateSeconds { get; set; } = 300;
}