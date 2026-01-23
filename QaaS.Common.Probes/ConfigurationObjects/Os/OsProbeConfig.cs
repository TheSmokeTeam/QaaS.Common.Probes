using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Os;

public record OsProbeConfig
{
    [Required, Description("The openshift environment to perform action in")]
    public Openshift? Openshift { get; set; }
}