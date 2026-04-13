using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Hooks.Probe;
using QaaS.Framework.SDK.Session.SessionDataObjects;
using RabbitMQ.Client;

namespace QaaS.Common.Probes.RabbitMqProbes;

public abstract class BaseRabbitMqObjectsManipulation<TRabbitMqObjectsManipulationConfig, TObjectToManipulateConfig> :
    BaseProbe<TRabbitMqObjectsManipulationConfig> where TRabbitMqObjectsManipulationConfig : BaseRabbitMqConfig, new()
{
    private IConnectionFactory _connectionFactory = null!;
    protected string RabbitmqConnectionString = "";

    protected virtual IConnectionFactory GetRabbitConnection()
    {
        RabbitmqConnectionString = $"amqp://{Configuration.Host}:{Configuration.Port} (vhost: {Configuration.VirtualHost})";
        _connectionFactory = new ConnectionFactory
        {
            HostName = Configuration.Host!,
            Port = Configuration.Port,
            UserName = Configuration.Username,
            Password = Configuration.Password,
            VirtualHost = Configuration.VirtualHost
        };
        return _connectionFactory;
    }

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
