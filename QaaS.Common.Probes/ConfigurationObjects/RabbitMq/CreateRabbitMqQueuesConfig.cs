using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

public record CreateRabbitMqQueuesConfig : BaseRabbitMqConfig
{
    [Required, MinLength(1), Description("The rabbitmq queues to create")]
    public RabbitMqQueueConfig[]? Queues { get; set; }
}