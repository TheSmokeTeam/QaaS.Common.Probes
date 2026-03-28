using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using RabbitMQ.Client;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Probe that deletes rabbitmq exchanges
/// </summary>
/// <qaas-docs group="RabbitMQ administration" subgroup="Exchanges lifecycle" />
public class DeleteRabbitMqExchanges : BaseRabbitMqObjectsManipulation<DeleteRabbitMqExchangesConfig, string>
{
    protected override IEnumerable<string> GetObjectsToManipulateConfigurations()
        => Configuration.ExchangeNames!;


    protected override void ManipulateObject(IChannel channel, string objectToManipulateConfig)
    {
        channel.ExchangeDeleteAsync(objectToManipulateConfig).GetAwaiter().GetResult();
        Context.Logger.LogDebug("Deleted exchange {ExchangeName} in the rabbitmq {RabbitmqConnectionString}"
            , objectToManipulateConfig, RabbitmqConnectionString);
    }
}
