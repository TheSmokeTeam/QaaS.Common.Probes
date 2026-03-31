using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;
using RabbitMQ.Client;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Creates one or more RabbitMQ queues with the configured queue arguments.
/// </summary>
/// <qaas-docs group="RabbitMQ administration" subgroup="Queues lifecycle" />
public class CreateRabbitMqQueues
    : BaseRabbitMqObjectsManipulationWithGlobalDictDefaults<CreateRabbitMqQueuesConfig, RabbitMqQueueConfig>
{
    protected override IEnumerable<ProbeGlobalDictReadRequest> GetAdditionalGlobalDictionaryReadRequests(
        IConfiguration localConfiguration)
    {
        yield return new ProbeGlobalDictReadRequest("recovery",
            BuildGlobalDictionaryAliasPath("RabbitMq", "Recovery", "Queues"));
    }

    protected override IEnumerable<RabbitMqQueueConfig> GetObjectsToManipulateConfigurations() => Configuration.Queues!;

    protected override void ManipulateObject(IChannel channel, RabbitMqQueueConfig objectToManipulateConfig)
    {
        channel.QueueDeclareAsync(objectToManipulateConfig.Name!, objectToManipulateConfig.Durable,
                objectToManipulateConfig.Exclusive, objectToManipulateConfig.AutoDelete,
                objectToManipulateConfig.Arguments)
            .GetAwaiter().GetResult();
        Context.Logger.LogDebug("Created queue {QueueName} in the rabbitmq {RabbitmqConnectionString}",
            objectToManipulateConfig.Name, RabbitmqConnectionString);
    }
}
