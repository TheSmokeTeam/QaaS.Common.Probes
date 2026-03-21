using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

public record CreateRabbitMqVirtualHostsConfig : BaseRabbitMqManagementConfig
{
    [Required, MinLength(1), Description("The rabbitmq virtual hosts to create or update")]
    public RabbitMqVirtualHostConfig[]? VirtualHosts { get; set; }
}
