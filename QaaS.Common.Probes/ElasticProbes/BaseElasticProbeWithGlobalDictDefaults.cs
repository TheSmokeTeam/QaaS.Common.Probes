using System.Collections.Immutable;
using Nest;
using QaaS.Common.Probes.ConfigurationObjects;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;
using QaaS.Framework.Protocols.ConfigurationObjects.Elastic;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Session.SessionDataObjects;

namespace QaaS.Common.Probes.ElasticProbes;

/// <summary>
/// Shared Elasticsearch probe base that can resolve missing cluster settings from the probe global dictionary before
/// opening the client connection.
/// </summary>
public abstract class BaseElasticProbeWithGlobalDictDefaults<TElasticProbeConfig>
    : BaseProbeWithGlobalDictDefaults<TElasticProbeConfig>
    where TElasticProbeConfig : BaseElasticConfig, IUseGlobalDictProbeConfiguration, new()
{
    protected IElasticClient ElasticClient = null!;

    private ElasticClient CreateConnection()
        => new(new ConnectionSettings(new Uri(Configuration.Url!))
                .BasicAuthentication(Configuration.Username!, Configuration.Password!)
                .RequestTimeout(TimeSpan.FromMilliseconds(Configuration.RequestTimeoutMs))
                .ServerCertificateValidationCallback((_, _, _, _) => true)
        );

    protected override IReadOnlyList<string> GetConfigurationDefaultsAliasPath()
        => BuildGlobalDictionaryAliasPath("Elastic", "Defaults");

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        ElasticClient = CreateConnection();
        RunElasticProbe();
    }

    protected abstract void RunElasticProbe();
}
