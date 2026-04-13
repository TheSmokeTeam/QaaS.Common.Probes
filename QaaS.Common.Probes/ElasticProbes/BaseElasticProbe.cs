using System.Collections.Immutable;
using Nest;
using QaaS.Common.Probes.ConfigurationObjects.Elastic;
using QaaS.Framework.Protocols.ConfigurationObjects.Elastic;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Hooks.Probe;
using QaaS.Framework.SDK.Session.SessionDataObjects;

namespace QaaS.Common.Probes.ElasticProbes;

public abstract class BaseElasticProbe<TElasticProbeConfig> : BaseProbe<TElasticProbeConfig> where
    TElasticProbeConfig : BaseElasticConfig, new()
{
    protected IElasticClient ElasticClient = null!;

    private ElasticClient CreateConnection()
    {
        var connectionSettings = new ConnectionSettings(new Uri(Configuration.Url!))
            .BasicAuthentication(Configuration.Username!, Configuration.Password!)
            .RequestTimeout(TimeSpan.FromMilliseconds(Configuration.RequestTimeoutMs));

        if (Configuration is IElasticTlsConfiguration tlsConfiguration &&
            tlsConfiguration.AllowInvalidServerCertificates)
        {
            connectionSettings = connectionSettings.ServerCertificateValidationCallback((_, _, _, _) => true);
        }

        return new ElasticClient(connectionSettings);
    }

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        ElasticClient = CreateConnection();
        RunElasticProbe();
    }

    protected abstract void RunElasticProbe();
}
