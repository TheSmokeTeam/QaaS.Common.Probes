using System.Collections.Immutable;
using QaaS.Common.Probes.ConfigurationObjects.MongoDb;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Session.SessionDataObjects;

namespace QaaS.Common.Probes.MongoDbProbes;

/// <summary>
/// Shared MongoDB probe base that resolves collection-target defaults from the probe global dictionary.
/// </summary>
public abstract class BaseMongoDbProbeWithGlobalDictDefaults<TMongoDbConfig>
    : BaseProbeWithGlobalDictDefaults<TMongoDbConfig>
    where TMongoDbConfig : QaaS.Framework.Configurations.CommonConfigurationObjects.MongoCollectionConfig,
        ConfigurationObjects.IUseGlobalDictProbeConfiguration, new()
{
    protected override IReadOnlyList<string> GetConfigurationDefaultsAliasPath()
        => BuildGlobalDictionaryAliasPath("MongoDb", "Defaults");

    public abstract override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList);
}
