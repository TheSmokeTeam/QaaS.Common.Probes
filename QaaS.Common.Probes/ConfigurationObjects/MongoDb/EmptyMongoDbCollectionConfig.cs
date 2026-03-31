using System.ComponentModel;
using QaaS.Common.Probes.ConfigurationObjects;
using QaaS.Framework.Configurations.CommonConfigurationObjects;

namespace QaaS.Common.Probes.ConfigurationObjects.MongoDb;

public record EmptyMongoDbCollectionConfig : MongoCollectionConfig, IUseGlobalDictProbeConfiguration
{
    [Description("When true, missing MongoDB probe configuration keys can be resolved from the shared global dictionary before local values are applied."),
     DefaultValue(false)]
    public bool UseGlobalDict { get; set; }
}
