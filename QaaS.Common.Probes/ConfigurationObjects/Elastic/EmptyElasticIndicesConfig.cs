using System.ComponentModel;
using QaaS.Common.Probes.ConfigurationObjects;
using QaaS.Framework.Protocols.ConfigurationObjects.Elastic;

namespace QaaS.Common.Probes.ConfigurationObjects.Elastic;

/// <summary>
/// Configuration for deleting Elasticsearch indices selected by regex, with optional global-dictionary fallback and TLS override support.
/// </summary>
public record EmptyElasticIndicesConfig : ElasticIndicesRegex, IUseGlobalDictProbeConfiguration,
    IElasticTlsConfiguration
{
    [Description("When true, missing Elasticsearch probe configuration keys can be resolved from the shared global dictionary before local values are applied."),
     DefaultValue(false)]
    public bool UseGlobalDict { get; set; }

    [Description("Allow invalid TLS certificates when connecting to Elasticsearch over HTTPS."),
     DefaultValue(false)]
    public bool AllowInvalidServerCertificates { get; set; }
}
