using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

public record DeleteRabbitMqQueuesConfig : BaseRabbitMqConfig
{
    [Required, MinLength(1), Description("A list of the names of all the queues to delete from the given rabbitmq")]
    public string[]? QueueNames { get; set; }
}