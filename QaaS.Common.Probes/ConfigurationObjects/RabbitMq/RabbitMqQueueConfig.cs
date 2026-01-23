using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

public record RabbitMqQueueConfig
{
    [Required, Description("The name of the queue")]
    public string? Name { get; set; }

    [Description("Should the exchange be durable"), DefaultValue(false)]
    public bool Durable { get; set; } = false;

    [Required, Description("Should the queue be exclusive"), DefaultValue(false)]
    public bool Exclusive { get; set; } = false;

    [Description("Should the queue be autoDelete"), DefaultValue(true)]
    public bool AutoDelete { get; set; } = true;

    [Description("Extra arguments for the queue"), DefaultValue(null)]
    public Dictionary<string, object?>? Arguments { get; set; } = null;
}