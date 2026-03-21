using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

public record DeleteRabbitMqPermissionsConfig : BaseRabbitMqManagementConfig
{
    [Required, MinLength(1), Description("The rabbitmq permission targets to delete")]
    public RabbitMqPermissionTargetConfig[]? Permissions { get; set; }
}
