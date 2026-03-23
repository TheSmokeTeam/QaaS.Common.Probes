using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using RabbitMQ.Client;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Creates one or more RabbitMQ exchanges with the configured durability, auto-delete, and arguments.
/// </summary>
public class
    CreateRabbitMqExchanges
    : BaseRabbitMqObjectsManipulation<CreateRabbitMqExchangesConfig, RabbitMqExchangeConfig>
{
    protected override IEnumerable<RabbitMqExchangeConfig> GetObjectsToManipulateConfigurations()
        => Configuration.Exchanges!;

    protected override void ManipulateObject(IChannel channel, RabbitMqExchangeConfig objectToManipulateConfig)
    {
        var exchangeType = objectToManipulateConfig.Type==RabbitMqExchangeType.ConsistentHash?"x-consistent-hash":objectToManipulateConfig.Type.ToString().ToLower();
        channel.ExchangeDeclareAsync(objectToManipulateConfig.Name!, exchangeType, objectToManipulateConfig.Durable,
            objectToManipulateConfig.AutoDelete, objectToManipulateConfig.Arguments).GetAwaiter().GetResult();

        Context.Logger.LogDebug(
            "Created exchange {ExchangeName} of type {ExchangeType} in the rabbitmq {RabbitmqConnectionString}"
            , objectToManipulateConfig.Name, exchangeType, RabbitmqConnectionString);
    }
}
