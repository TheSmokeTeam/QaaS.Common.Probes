using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Creates one or more RabbitMQ exchanges with the configured durability, auto-delete, and arguments.
/// </summary>
/// <qaas-docs group="RabbitMQ administration" subgroup="Exchanges lifecycle" />
public class CreateRabbitMqExchanges
    : BaseRabbitMqObjectsManipulationWithGlobalDict<CreateRabbitMqExchangesConfig, RabbitMqExchangeConfig>
{
    protected override IEnumerable<ProbeGlobalDictReadRequest> GetAdditionalGlobalDictionaryReadRequests(
        IConfiguration localConfiguration)
    {
        yield return new ProbeGlobalDictReadRequest("recovery",
            BuildGlobalDictionaryAliasPath("RabbitMq", "Recovery", "Exchanges"));
    }

    protected override IEnumerable<RabbitMqExchangeConfig> GetObjectsToManipulateConfigurations()
        => Configuration.Exchanges!;

    /// <summary>
    /// Declares the requested exchange and converts broker precondition failures into configuration-mismatch errors that
    /// include the requested exchange shape.
    /// </summary>
    protected override void ManipulateObject(IChannel channel, RabbitMqExchangeConfig objectToManipulateConfig)
    {
        var exchangeType = objectToManipulateConfig.Type == RabbitMqExchangeType.ConsistentHash
            ? "x-consistent-hash"
            : objectToManipulateConfig.Type.ToString().ToLower();
        try
        {
            channel.ExchangeDeclareAsync(objectToManipulateConfig.Name!, exchangeType, objectToManipulateConfig.Durable,
                objectToManipulateConfig.AutoDelete, objectToManipulateConfig.Arguments).GetAwaiter().GetResult();
        }
        catch (OperationInterruptedException exception)
            when (RabbitMqDeclarationValidation.IsConfigurationMismatch(exception))
        {
            throw RabbitMqDeclarationValidation.CreateConfigurationMismatchException("exchange",
                objectToManipulateConfig.Name!, DescribeRequestedExchangeConfiguration(objectToManipulateConfig),
                exception);
        }
        catch (AlreadyClosedException exception)
            when (RabbitMqDeclarationValidation.IsConfigurationMismatch(exception))
        {
            throw RabbitMqDeclarationValidation.CreateConfigurationMismatchException("exchange",
                objectToManipulateConfig.Name!, DescribeRequestedExchangeConfiguration(objectToManipulateConfig),
                exception);
        }

        Context.Logger.LogDebug(
            "Created exchange {ExchangeName} of type {ExchangeType} in the rabbitmq {RabbitmqConnectionString}",
            objectToManipulateConfig.Name, exchangeType, RabbitmqConnectionString);
    }

    private static string DescribeRequestedExchangeConfiguration(RabbitMqExchangeConfig exchangeConfig)
        => $"type={(exchangeConfig.Type == RabbitMqExchangeType.ConsistentHash ? "x-consistent-hash" : exchangeConfig.Type.ToString().ToLower())}, durable={exchangeConfig.Durable}, autoDelete={exchangeConfig.AutoDelete}, arguments={RabbitMqDeclarationValidation.FormatArguments(exchangeConfig.Arguments)}";
}
