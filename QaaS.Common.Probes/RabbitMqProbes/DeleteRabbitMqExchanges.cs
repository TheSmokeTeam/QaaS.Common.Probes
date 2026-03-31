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
    protected override IEnumerable<string> GetObjectsToManipulateConfigurations()
        => Configuration.ExchangeNames!;

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        base.Run(sessionDataList, dataSourceList);
        if (Configuration.UseGlobalDict)
        {
            SaveGlobalDictionaryPayload("recovery",
                new
                {
                    Exchanges = Configuration.ExchangeNames!
                        .Select(exchangeName => new RabbitMqExchangeConfig { Name = exchangeName })
                        .ToArray()
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
}
