using System.ComponentModel;
using QaaS.Common.Probes.ConfigurationObjects;
using QaaS.Framework.Protocols.ConfigurationObjects.Elastic;

namespace QaaS.Common.Probes.ConfigurationObjects.Elastic;

public record DeleteElasticIndicesConfig : BaseElasticIndices, IUseGlobalDictProbeConfiguration,
    IElasticTlsConfiguration
{
    [Description("When true, missing Elasticsearch probe configuration keys can be resolved from the shared global dictionary before local values are applied."),
     DefaultValue(false)]
    public bool UseGlobalDict { get; set; }

    [Description("Allow invalid TLS certificates when connecting to Elasticsearch over HTTPS."),
     DefaultValue(false)]
    public bool AllowInvalidServerCertificates { get; set; }
}
