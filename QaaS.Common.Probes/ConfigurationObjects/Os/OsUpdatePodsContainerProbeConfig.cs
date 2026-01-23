using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Os;

public record OsUpdatePodsContainerProbeConfig : OsUpdatePodsProbeConfig
{
    [Required, Description("The name of the container to update")]
    public string? ContainerName { get; set; }
}