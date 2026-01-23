using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

/// <summary>
/// Configuration object for multiple rabbitmq bindings on a rabbitmq server
/// </summary>
public record RabbitMqBindingsConfig : BaseRabbitMqConfig
{
    [Required, MinLength(1), Description("The rabbitmq bindings")]
    public RabbitMqBindingConfig[]? Bindings { get; set; }
}