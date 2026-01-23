using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Os;

public record OsRestartPodsConfig : OsProbeConfig
{
    [Required, Description("A list of the k8s labels of the pods to execute the command in, for example: app=test")]
    public string[]? ApplicationLabels { get; set; }

    [Range(0, int.MaxValue), Description(
         "The interval in milliseconds between every check of the pod's state (if they are ready yet)"),
     DefaultValue(1000)]
    public int IntervalBetweenDesiredStateChecksMs { get; set; } = 1000;

    [Range(0, int.MaxValue), Description("The timeout in seconds for waiting for the pods to stop restarting," +
                                         " when timeout is reached an error log is raised and the code continues to run"),
     DefaultValue(300)]
    public int TimeoutWaitForDesiredStateSeconds { get; set; } = 300;
}