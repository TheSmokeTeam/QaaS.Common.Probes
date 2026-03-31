using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;
using RabbitMQ.Client;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Creates one or more RabbitMQ exchanges with the configured durability, auto-delete, and arguments.
/// </summary>
/// <qaas-docs group="RabbitMQ administration" subgroup="Exchanges lifecycle" />
public class CreateRabbitMqExchanges
    : BaseRabbitMqObjectsManipulationWithGlobalDict<CreateRabbitMqExchangesConfig, RabbitMqExchangeConfig>
{
    protected override IEnumerable<ProbeGlobalDictReadRequest> GetAdditionalGlobalDictionaryReadRequests(
        IConfiguration localConfiguration)
    {
        yield return new ProbeGlobalDictReadRequest("recovery",
            BuildGlobalDictionaryAliasPath("RabbitMq", "Recovery", "Exchanges"));
    }

    protected override IEnumerable<RabbitMqExchangeConfig> GetObjectsToManipulateConfigurations()
        => Configuration.Exchanges!;

    protected override void ManipulateObject(IChannel channel, RabbitMqExchangeConfig objectToManipulateConfig)
    {
        var exchangeType = objectToManipulateConfig.Type == RabbitMqExchangeType.ConsistentHash
            ? "x-consistent-hash"
            : objectToManipulateConfig.Type.ToString().ToLower();
        channel.ExchangeDeclareAsync(objectToManipulateConfig.Name!, exchangeType, objectToManipulateConfig.Durable,
            objectToManipulateConfig.AutoDelete, objectToManipulateConfig.Arguments).GetAwaiter().GetResult();

        Context.Logger.LogDebug(
            "Created exchange {ExchangeName} of type {ExchangeType} in the rabbitmq {RabbitmqConnectionString}",
            objectToManipulateConfig.Name, exchangeType, RabbitmqConnectionString);
    }
}
