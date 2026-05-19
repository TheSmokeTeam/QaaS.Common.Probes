using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Os;

public record OsUpdatePodsProbeConfig : OsProbeConfig
{
    [Required, Description("Legacy property name; supply the target Deployment or StatefulSet name for OpenShift update probes.")]
    public string? ReplicaSetName { get; set; }

    [Range(0, int.MaxValue), Description(
         "Interval in milliseconds between desired-state checks."),
     DefaultValue(1000)]
    public int IntervalBetweenDesiredStateChecksMs { get; set; } = 1000;

    [Range(0, int.MaxValue), Description(
         "Timeout in seconds for waiting for the workload to reach the desired state; when reached, the probe throws TimeoutException."),
     DefaultValue(300)]
    public int TimeoutWaitForDesiredStateSeconds { get; set; } = 300;
}
