using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

public record DeleteRabbitMqVirtualHostsConfig : BaseRabbitMqManagementConfig
{
    [Required, MinLength(1), Description("The rabbitmq virtual host names to delete")]
    public string[]? VirtualHostNames { get; set; }
}
