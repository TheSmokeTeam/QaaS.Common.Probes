using System.ComponentModel;
using QaaS.Common.Probes.ConfigurationObjects;
using QaaS.Framework.Protocols.ConfigurationObjects.Elastic;

namespace QaaS.Common.Probes.ConfigurationObjects.Elastic;

public record EmptyElasticIndicesConfig : ElasticIndicesRegex, IUseGlobalDictProbeConfiguration
{
    [Description("When true, missing Elasticsearch probe configuration keys can be resolved from the shared global dictionary before local values are applied."),
     DefaultValue(false)]
    public bool UseGlobalDict { get; set; }
}
