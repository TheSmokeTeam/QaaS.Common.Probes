using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

public record DeleteRabbitMqUsersConfig : BaseRabbitMqManagementConfig
{
    [Required, MinLength(1), Description("The rabbitmq users to delete")]
    public string[]? Usernames { get; set; }
}
