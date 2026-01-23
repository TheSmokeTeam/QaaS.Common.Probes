using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Os;

public record OsUpdateImageProbeConfig : OsUpdatePodsContainerProbeConfig
{
    [Required, Description("The desired image to update the container to")]
    public string? DesiredImage { get; set; }
}