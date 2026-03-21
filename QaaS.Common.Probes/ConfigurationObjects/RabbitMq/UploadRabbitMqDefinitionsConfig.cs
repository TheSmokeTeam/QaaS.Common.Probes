using System.ComponentModel;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

public record UploadRabbitMqDefinitionsConfig : BaseRabbitMqManagementConfig
{
    [Description("Optional inline JSON payload with rabbitmq definitions")]
    public string? DefinitionsJson { get; set; }

    [Description("Optional path to a JSON file containing rabbitmq definitions")]
    public string? DefinitionsFilePath { get; set; }

    [Description("Optional virtual host name for vhost-scoped definitions import")]
    public string? VirtualHostName { get; set; }
}
