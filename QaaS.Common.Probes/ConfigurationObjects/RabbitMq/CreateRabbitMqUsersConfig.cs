using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

public record CreateRabbitMqUsersConfig : BaseRabbitMqManagementConfig
{
    [Required, MinLength(1), Description("The rabbitmq users to create or update")]
    public RabbitMqUserConfig[]? Users { get; set; }
}
