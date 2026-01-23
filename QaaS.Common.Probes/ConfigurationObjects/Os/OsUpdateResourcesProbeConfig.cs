using System.ComponentModel;

namespace QaaS.Common.Probes.ConfigurationObjects.Os;

public record ResourceUnit
{
    [Description("The amount of cpu to update the replicaset with")]
    public string? Cpu { get; set; } = null;

    [Description("The amount of memory to update the replicaset with")]
    public string? Memory { get; set; } = null;
}

public record Resources
{
    [Description("The requests resources to update the replicaset with")]
    public ResourceUnit? Requests { get; set; } = null;

    [Description("The limits resources to update the replicaset with")]
    public ResourceUnit? Limits { get; set; } = null;
}

public record OsUpdateResourcesProbeConfig : OsUpdatePodsContainerProbeConfig
{
    [Description("The resources to update the replicaset with. Overrides the current replicaset's resources.")]
    public Resources? DesiredResources { get; set; } = null;
}