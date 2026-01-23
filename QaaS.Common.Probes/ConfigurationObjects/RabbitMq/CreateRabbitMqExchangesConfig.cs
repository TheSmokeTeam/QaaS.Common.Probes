using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

public record CreateRabbitMqExchangesConfig : BaseRabbitMqConfig
{
    [Required, MinLength(1), Description("The rabbitmq exchanges to create")]
    public RabbitMqExchangeConfig[]? Exchanges { get; set; }
}