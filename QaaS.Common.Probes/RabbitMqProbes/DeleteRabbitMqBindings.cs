using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using RabbitMQ.Client;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Deletes RabbitMQ bindings using the configured source, destination, routing key, and binding type.
/// </summary>
public class DeleteRabbitMqBindings
    : BaseRabbitMqObjectsManipulation<RabbitMqBindingsConfig, RabbitMqBindingConfig>
{
    protected override IEnumerable<RabbitMqBindingConfig> GetObjectsToManipulateConfigurations()
        => Configuration.Bindings!;


    protected override void ManipulateObject(IChannel channel, RabbitMqBindingConfig objectToManipulateConfig)
    {
        Context.Logger.LogDebug("Deleting {BindingType} binding between {SourceName} and {DestName} with a routing " +
                                "key of {RoutingKey}", objectToManipulateConfig.BindingType.ToString(),
            objectToManipulateConfig.SourceName, objectToManipulateConfig.DestinationName,
            objectToManipulateConfig.RoutingKey);

        switch (objectToManipulateConfig.BindingType)
        {
            case BindingType.ExchangeToQueue:
                channel.QueueUnbindAsync(objectToManipulateConfig.DestinationName!,
                    objectToManipulateConfig.SourceName!,
                    objectToManipulateConfig.RoutingKey, objectToManipulateConfig.Arguments).GetAwaiter().GetResult();
                break;
            case BindingType.ExchangeToExchange:
                channel.ExchangeUnbindAsync(objectToManipulateConfig.DestinationName!,
                    objectToManipulateConfig.SourceName!,
                    objectToManipulateConfig.RoutingKey, objectToManipulateConfig.Arguments).GetAwaiter().GetResult();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(objectToManipulateConfig.BindingType),
                    objectToManipulateConfig.BindingType, "Binding type not supported");
        }
    }
}
