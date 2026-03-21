using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

public record RabbitMqVirtualHostConfig
{
    [Required, Description("The name of the rabbitmq virtual host")]
    public string? Name { get; set; }

    [Description("Optional description for the rabbitmq virtual host")]
    public string? Description { get; set; }

    [Description("Optional tags for the rabbitmq virtual host")]
    public string[]? Tags { get; set; }

    [Description("Optional default queue type for the virtual host")]
    public string? DefaultQueueType { get; set; }

    [Description("Optional deletion protection flag for the virtual host")]
    public bool? ProtectedFromDeletion { get; set; }

    [Description("Optional tracing flag for the virtual host")]
    public bool? Tracing { get; set; }
}
