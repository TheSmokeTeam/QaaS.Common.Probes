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
    private CreateRabbitMqQueuesConfig? _previousDefaults;
    private QueueRecoveryPayload? _previousRecovery;

    protected override IEnumerable<string> GetObjectsToManipulateConfigurations() => Configuration.QueueNames!;

    protected override void SaveResolvedConfigurationDefaults()
    {
        if (Configuration.UseGlobalDict)
        {
            _previousRecovery = RabbitMqRecoverySnapshotHelper.TryGetRecoveryPayload<QueueRecoveryPayload>(Context,
                BuildGlobalDictionaryAliasPath("RabbitMq", "Recovery", "Queues"));
            _previousDefaults = RabbitMqRecoverySnapshotHelper.TryGetConfigurationDefaults<CreateRabbitMqQueuesConfig>(
                Context,
                BuildGlobalDictionaryAliasPath("RabbitMq", "AmqpDefaults"));
        }

        base.SaveResolvedConfigurationDefaults();
    }

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        base.Run(sessionDataList, dataSourceList);
        if (Configuration.UseGlobalDict)
        {
            var queueNames = Configuration.QueueNames!;
            var queues = RabbitMqRecoverySnapshotHelper.FilterByNames(
                _previousRecovery?.Queues ?? _previousDefaults?.Queues,
                queueNames,
                queue => queue.Name);
            if (queues.Length == 0)
            {
                queues = queueNames
                    .Select(queueName => new RabbitMqQueueConfig { Name = queueName })
                    .ToArray();
            }

            SaveGlobalDictionaryPayload("recovery",
                new QueueRecoveryPayload
                {
                    Queues = queues
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

    private sealed record QueueRecoveryPayload
    {
        public RabbitMqQueueConfig[]? Queues { get; init; }
    }
}
