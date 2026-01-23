using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

public record DeleteRabbitMqExchangesConfig : BaseRabbitMqConfig
{
    [Required, MinLength(1), Description("A list of the names of all the exchanges to delete from the given rabbitmq")]
    public string[]? ExchangeNames { get; set; }
}