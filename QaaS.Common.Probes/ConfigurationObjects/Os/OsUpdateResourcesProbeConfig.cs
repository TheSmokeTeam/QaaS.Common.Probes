using System.ComponentModel;

namespace QaaS.Common.Probes.ConfigurationObjects.Os;

public record ResourceUnit
{
    [Description("Desired CPU value.")]
    public string? Cpu { get; set; } = null;

    [Description("Desired memory value.")]
    public string? Memory { get; set; } = null;
}

public record Resources
{
    [Description("Desired CPU and memory requests.")]
    public ResourceUnit? Requests { get; set; } = null;

    [Description("Desired CPU and memory limits.")]
    public ResourceUnit? Limits { get; set; } = null;
}

public record OsUpdateResourcesProbeConfig : OsUpdatePodsContainerProbeConfig
{
    [Description("Desired target workload container requests and limits. Only cpu and memory are rebuilt by the implementation.")]
    public Resources? DesiredResources { get; set; } = null;
}
