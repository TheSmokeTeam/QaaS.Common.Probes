using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Os;

public record OsScalePodsProbeConfig : OsUpdatePodsProbeConfig
{
    [Required, Description("Number of replicas to set on the target Deployment or StatefulSet. Set it explicitly; there is no local range validation.")]
    public int DesiredNumberOfPods { get; set; }
}
