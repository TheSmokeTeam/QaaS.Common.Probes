using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

public record UpsertRabbitMqPermissionsConfig : BaseRabbitMqManagementConfig
{
    [Required, MinLength(1), Description("The rabbitmq permissions to create or update")]
    public RabbitMqPermissionConfig[]? Permissions { get; set; }
}
