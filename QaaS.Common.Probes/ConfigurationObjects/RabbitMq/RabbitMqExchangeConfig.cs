using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

public enum RabbitMqExchangeType
{
    Fanout,
    Topic,
    Direct,
    Headers,
    ConsistentHash
}

public record RabbitMqExchangeConfig
{
    [Required, Description("The name of the exchange")]
    public string? Name { get; set; }

    [Description("The type of the exchange"), DefaultValue(RabbitMqExchangeType.Fanout)]
    public RabbitMqExchangeType Type { get; set; } = RabbitMqExchangeType.Fanout;

    [Description("Should the exchange be durable"), DefaultValue(false)]
    public bool Durable { get; set; } = false;

    [Description("Should the exchange be autoDelete"), DefaultValue(false)]
    public bool AutoDelete { get; set; } = false;

    [Description("Extra arguments for the exchange"), DefaultValue(null)]
    public Dictionary<string, object?>? Arguments { get; set; } = null;
}