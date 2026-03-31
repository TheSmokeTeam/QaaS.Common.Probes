using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Session.SessionDataObjects;
using RabbitMQ.Client;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Probe that deletes rabbitmq queues
/// </summary>
/// <qaas-docs group="RabbitMQ administration" subgroup="Queues lifecycle" />
public class DeleteRabbitMqQueues
    : BaseRabbitMqObjectsManipulationWithGlobalDict<DeleteRabbitMqQueuesConfig, string>
{
    protected override IEnumerable<string> GetObjectsToManipulateConfigurations() => Configuration.QueueNames!;

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        base.Run(sessionDataList, dataSourceList);
        if (Configuration.UseGlobalDict)
        {
            SaveGlobalDictionaryPayload("recovery",
                new
                {
                    Queues = Configuration.QueueNames!
                        .Select(queueName => new RabbitMqQueueConfig { Name = queueName })
                        .ToArray()
                },
                BuildGlobalDictionaryAliasPath("RabbitMq", "Recovery", "Queues"));
        }
    }

    protected override void ManipulateObject(IChannel channel, string objectToManipulateConfig)
    {
        channel.QueueDeleteAsync(objectToManipulateConfig).GetAwaiter().GetResult();
        Context.Logger.LogDebug("Deleted queue {QueueName} in the rabbitmq {RabbitmqConnectionString}",
            objectToManipulateConfig, RabbitmqConnectionString);
    }
}
