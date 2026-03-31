using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using QaaS.Common.Probes.ConfigurationObjects;

namespace QaaS.Common.Probes.ConfigurationObjects.Os;

public record OsProbeConfig : IUseGlobalDictProbeConfiguration
{
    [Description("When true, missing OpenShift probe configuration keys can be resolved from the shared global dictionary before local values are applied."),
     DefaultValue(false)]
    public bool UseGlobalDict { get; set; }

    [Required, Description("The openshift environment to perform action in")]
    public Openshift? Openshift { get; set; }
}
