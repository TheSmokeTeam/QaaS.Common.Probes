using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

public record RabbitMqPermissionTargetConfig
{
    [Required, Description("The rabbitmq virtual host name")]
    public string? VirtualHostName { get; set; }

    [Required, Description("The rabbitmq user name")]
    public string? Username { get; set; }
}
