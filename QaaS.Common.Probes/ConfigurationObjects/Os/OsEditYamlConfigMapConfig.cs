using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Os;

public record OsEditYamlConfigMapConfig : OsProbeConfig
{
    [Required, Description("The config map to edit")]
    public string? ConfigMapName { get; set; }

    [Description("The name of the yaml file inside the config map data"), DefaultValue("ConfigMap.yml")]
    public string ConfigMapYamlFileName { get; set; } = "ConfigMap.yml";

    [Description("The description of the configmap paths and values to change (in JSONPath format). For example:" +
                 "path.to.yaml.value[0]: new value")]
    public Dictionary<string, object> ValuesToEdit { get; set; }
}