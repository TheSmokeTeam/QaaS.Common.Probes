using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

public enum BindingType
{
    ExchangeToQueue,
    ExchangeToExchange
}

public record RabbitMqBindingConfig
{
    [Required, Description("The name of the binding's source")]
    public string? SourceName { get; set; }

    [Required, Description("The name of the binding's destination")]
    public string? DestinationName { get; set; }

    [Description("The binding's routing key"), DefaultValue("/")]
    public string RoutingKey { get; set; } = "/";

    [Description("The binding's arguments"), DefaultValue(null)]
    public Dictionary<string, object?>? Arguments { get; set; } = null;

    [Description("The type of the binding"), DefaultValue(BindingType.ExchangeToQueue)]
    public BindingType BindingType { get; set; } = BindingType.ExchangeToQueue;
}