using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;
using RabbitMQ.Client;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Creates RabbitMQ bindings between exchanges and queues or between exchanges by using the configured binding definitions.
/// </summary>
/// <qaas-docs group="RabbitMQ administration" subgroup="Bindings lifecycle" />
public class CreateRabbitMqBindings
    : BaseRabbitMqObjectsManipulationWithGlobalDictDefaults<RabbitMqBindingsConfig, RabbitMqBindingConfig>
{
    protected override IEnumerable<ProbeGlobalDictReadRequest> GetAdditionalGlobalDictionaryReadRequests(
        IConfiguration localConfiguration)
    {
        yield return new ProbeGlobalDictReadRequest("recovery",
            BuildGlobalDictionaryAliasPath("RabbitMq", "Recovery", "Bindings"));
    }

    protected override IEnumerable<RabbitMqBindingConfig> GetObjectsToManipulateConfigurations() =>
        Configuration.Bindings!;

    protected override void ManipulateObject(IChannel channel, RabbitMqBindingConfig objectToManipulateConfig)
    {
        Context.Logger.LogDebug("Creating {BindingType} binding between {SourceName} and {DestName} with a routing " +
                                "key of {RoutingKey}", objectToManipulateConfig.BindingType.ToString(),
            objectToManipulateConfig.SourceName, objectToManipulateConfig.DestinationName,
            objectToManipulateConfig.RoutingKey);

        switch (objectToManipulateConfig.BindingType)
        {
            case BindingType.ExchangeToQueue:
                channel.QueueBindAsync(objectToManipulateConfig.DestinationName!, objectToManipulateConfig.SourceName!,
                        objectToManipulateConfig.RoutingKey, arguments: objectToManipulateConfig.Arguments).GetAwaiter()
                    .GetResult();
                break;
            case BindingType.ExchangeToExchange:
                channel.ExchangeBindAsync(objectToManipulateConfig.DestinationName!,
                        objectToManipulateConfig.SourceName!,
                        objectToManipulateConfig.RoutingKey, arguments: objectToManipulateConfig.Arguments).GetAwaiter()
                    .GetResult();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(objectToManipulateConfig.BindingType),
                    objectToManipulateConfig.BindingType, "Binding type not supported");
        }
    }
}
