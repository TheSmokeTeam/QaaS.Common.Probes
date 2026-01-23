using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Os;

public record OsScalePodsProbeConfig : OsUpdatePodsProbeConfig
{
    [Required, Description("The number of pods to scale the replica set to")]
    public int DesiredNumberOfPods { get; set; }
}