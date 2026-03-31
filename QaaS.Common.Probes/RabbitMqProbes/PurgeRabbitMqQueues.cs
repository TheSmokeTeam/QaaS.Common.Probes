using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using RabbitMQ.Client;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Probe that purges rabbitmq queues
/// </summary>
/// <qaas-docs group="RabbitMQ administration" subgroup="Queues lifecycle" />
public class PurgeRabbitMqQueues
    : BaseRabbitMqObjectsManipulationWithGlobalDict<PurgeRabbitMqQueuesConfig, string>
{
    protected override IEnumerable<string> GetObjectsToManipulateConfigurations()
        => Configuration.QueueNames!;

    protected override void ManipulateObject(IChannel channel, string objectToManipulateConfig)
    {
        var purgedMessages = channel.QueuePurgeAsync(objectToManipulateConfig).GetAwaiter().GetResult();
        Context.Logger.LogDebug(
            "Purged {PurgedMessages} messages from queue {QueueName} in the rabbitmq {RabbitmqConnectionString}",
            purgedMessages, objectToManipulateConfig, RabbitmqConnectionString);
    }
}
