using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using RabbitMQ.Client;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Probe that deletes rabbitmq queues
/// </summary>
public class DeleteRabbitMqQueues
    : BaseRabbitMqObjectsManipulation<DeleteRabbitMqQueuesConfig, string>
{
    protected override IEnumerable<string> GetObjectsToManipulateConfigurations()
        => Configuration.QueueNames!;


    protected override void ManipulateObject(IChannel channel, string objectToManipulateConfig)
    {
        channel.QueueDeleteAsync(objectToManipulateConfig).GetAwaiter().GetResult();
        Context.Logger.LogDebug("Deleted queue {QueueName} in the rabbitmq {RabbitmqConnectionString}"
            , objectToManipulateConfig, RabbitmqConnectionString);
    }
}