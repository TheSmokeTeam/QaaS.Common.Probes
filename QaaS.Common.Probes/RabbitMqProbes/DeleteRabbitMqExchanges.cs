using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Session.SessionDataObjects;
using RabbitMQ.Client;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Probe that deletes rabbitmq exchanges
/// </summary>
/// <qaas-docs group="RabbitMQ administration" subgroup="Exchanges lifecycle" />
public class DeleteRabbitMqExchanges
    : BaseRabbitMqObjectsManipulationWithGlobalDict<DeleteRabbitMqExchangesConfig, string>
{
    private CreateRabbitMqExchangesConfig? _previousDefaults;
    private ExchangeRecoveryPayload? _previousRecovery;

    protected override IEnumerable<string> GetObjectsToManipulateConfigurations()
        => Configuration.ExchangeNames!;

    protected override void SaveResolvedConfigurationDefaults()
    {
        if (Configuration.UseGlobalDict)
        {
            _previousRecovery = RabbitMqRecoverySnapshotHelper.TryGetRecoveryPayload<ExchangeRecoveryPayload>(Context,
                BuildGlobalDictionaryAliasPath("RabbitMq", "Recovery", "Exchanges"));
            _previousDefaults =
                RabbitMqRecoverySnapshotHelper.TryGetConfigurationDefaults<CreateRabbitMqExchangesConfig>(Context,
                    BuildGlobalDictionaryAliasPath("RabbitMq", "AmqpDefaults"));
        }

        base.SaveResolvedConfigurationDefaults();
    }

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        base.Run(sessionDataList, dataSourceList);
        if (Configuration.UseGlobalDict)
        {
            var exchangeNames = Configuration.ExchangeNames!;
            var exchanges = RabbitMqRecoverySnapshotHelper.FilterByNames(
                _previousRecovery?.Exchanges ?? _previousDefaults?.Exchanges,
                exchangeNames,
                exchange => exchange.Name);
            if (exchanges.Length == 0)
            {
                exchanges = exchangeNames
                    .Select(exchangeName => new RabbitMqExchangeConfig { Name = exchangeName })
                    .ToArray();
            }

            SaveGlobalDictionaryPayload("recovery",
                new ExchangeRecoveryPayload
                {
                    Exchanges = exchanges
                },
                BuildGlobalDictionaryAliasPath("RabbitMq", "Recovery", "Exchanges"));
        }
    }

    protected override void ManipulateObject(IChannel channel, string objectToManipulateConfig)
    {
        channel.ExchangeDeleteAsync(objectToManipulateConfig).GetAwaiter().GetResult();
        Context.Logger.LogDebug("Deleted exchange {ExchangeName} in the rabbitmq {RabbitmqConnectionString}",
            objectToManipulateConfig, RabbitmqConnectionString);
    }

    private sealed record ExchangeRecoveryPayload
    {
        public RabbitMqExchangeConfig[]? Exchanges { get; init; }
    }
}
