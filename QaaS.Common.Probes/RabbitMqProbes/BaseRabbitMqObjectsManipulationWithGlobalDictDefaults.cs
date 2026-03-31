using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Session.SessionDataObjects;
using RabbitMQ.Client;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Shared AMQP-based RabbitMQ probe base that can consume session-scoped defaults and recovery payloads from the
/// probe global dictionary before manipulating broker objects.
/// </summary>
public abstract class BaseRabbitMqObjectsManipulationWithGlobalDict<TRabbitMqObjectsManipulationConfig,
    TObjectToManipulateConfig> : BaseProbeWithGlobalDict<TRabbitMqObjectsManipulationConfig>
    where TRabbitMqObjectsManipulationConfig : BaseRabbitMqConfig, new()
{
    private IConnectionFactory _connectionFactory = null!;
    protected string RabbitmqConnectionString = "";

    protected virtual IConnectionFactory GetRabbitConnection()
    {
        RabbitmqConnectionString =
            $"amqp://{Configuration.Username}:{Configuration.Password}@{Configuration.Host}:{Configuration.Port}";
        _connectionFactory = new ConnectionFactory
        {
            Uri = new Uri(RabbitmqConnectionString),
            VirtualHost = Configuration.VirtualHost
        };
        return _connectionFactory;
    }

    protected override IReadOnlyList<string> GetConfigurationDefaultsAliasPath()
        => BuildGlobalDictionaryAliasPath("RabbitMq", "AmqpDefaults");

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        var rabbitFactory = GetRabbitConnection();
        using var connection = rabbitFactory.CreateConnectionAsync().GetAwaiter().GetResult();
        using var channel = connection.CreateChannelAsync().GetAwaiter().GetResult();
        foreach (var objectToManipulateConfig in GetObjectsToManipulateConfigurations())
            ManipulateObject(channel, objectToManipulateConfig);


        Context.Logger.LogInformation("Preformed action of type {ActionType}" +
                                      " In the rabbitmq {RabbitmqConnectionString}",
            GetType().Name, RabbitmqConnectionString);
    }

    protected abstract IEnumerable<TObjectToManipulateConfig> GetObjectsToManipulateConfigurations();

    protected abstract void ManipulateObject(IChannel channel, TObjectToManipulateConfig objectToManipulateConfig);
}
