using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using RabbitMQ.Client;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Creates one or more RabbitMQ queues with the configured queue arguments.
/// </summary>
/// <qaas-docs group="RabbitMQ administration" subgroup="Queues lifecycle" />
public class CreateRabbitMqQueues : BaseRabbitMqObjectsManipulation<CreateRabbitMqQueuesConfig, RabbitMqQueueConfig>
{
    protected override IEnumerable<RabbitMqQueueConfig> GetObjectsToManipulateConfigurations() => Configuration.Queues!;


    protected override void ManipulateObject(IChannel channel, RabbitMqQueueConfig objectToManipulateConfig)
    {
        channel.QueueDeclareAsync(objectToManipulateConfig.Name!, objectToManipulateConfig.Durable,
                objectToManipulateConfig.Exclusive, objectToManipulateConfig.AutoDelete,
                objectToManipulateConfig.Arguments)
            .GetAwaiter().GetResult();
        Context.Logger.LogDebug("Created queue {QueueName} in the rabbitmq {RabbitmqConnectionString}"
            , objectToManipulateConfig.Name, RabbitmqConnectionString);
    }
}
