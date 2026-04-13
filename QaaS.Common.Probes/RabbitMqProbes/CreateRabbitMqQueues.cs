using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Creates one or more RabbitMQ queues with the configured queue arguments.
/// </summary>
/// <qaas-docs group="RabbitMQ administration" subgroup="Queues lifecycle" />
public class CreateRabbitMqQueues
    : BaseRabbitMqObjectsManipulationWithGlobalDict<CreateRabbitMqQueuesConfig, RabbitMqQueueConfig>
{
    protected override IEnumerable<ProbeGlobalDictReadRequest> GetAdditionalGlobalDictionaryReadRequests(
        IConfiguration localConfiguration)
    {
        yield return new ProbeGlobalDictReadRequest("recovery",
            BuildGlobalDictionaryAliasPath("RabbitMq", "Recovery", "Queues"));
    }

    protected override IEnumerable<RabbitMqQueueConfig> GetObjectsToManipulateConfigurations() => Configuration.Queues!;

    /// <summary>
    /// Declares the requested queue and converts broker precondition failures into configuration-mismatch errors that
    /// point back to the requested queue shape.
    /// </summary>
    protected override void ManipulateObject(IChannel channel, RabbitMqQueueConfig objectToManipulateConfig)
    {
        try
        {
            channel.QueueDeclareAsync(objectToManipulateConfig.Name!, objectToManipulateConfig.Durable,
                    objectToManipulateConfig.Exclusive, objectToManipulateConfig.AutoDelete,
                    objectToManipulateConfig.Arguments)
                .GetAwaiter().GetResult();
        }
        catch (OperationInterruptedException exception)
            when (RabbitMqDeclarationValidation.IsConfigurationMismatch(exception))
        {
            throw RabbitMqDeclarationValidation.CreateConfigurationMismatchException("queue",
                objectToManipulateConfig.Name!, DescribeRequestedQueueConfiguration(objectToManipulateConfig), exception);
        }
        catch (AlreadyClosedException exception)
            when (RabbitMqDeclarationValidation.IsConfigurationMismatch(exception))
        {
            throw RabbitMqDeclarationValidation.CreateConfigurationMismatchException("queue",
                objectToManipulateConfig.Name!, DescribeRequestedQueueConfiguration(objectToManipulateConfig), exception);
        }

        Context.Logger.LogDebug("Created queue {QueueName} in the rabbitmq {RabbitmqConnectionString}",
            objectToManipulateConfig.Name, RabbitmqConnectionString);
    }

    private static string DescribeRequestedQueueConfiguration(RabbitMqQueueConfig queueConfig)
        => $"durable={queueConfig.Durable}, exclusive={queueConfig.Exclusive}, autoDelete={queueConfig.AutoDelete}, arguments={RabbitMqDeclarationValidation.FormatArguments(queueConfig.Arguments)}";
}
