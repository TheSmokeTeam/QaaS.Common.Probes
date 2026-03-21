using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

public record DownloadRabbitMqDefinitionsConfig : BaseRabbitMqManagementConfig
{
    [Required, Description("Output path for the downloaded rabbitmq definitions JSON")]
    public string? DefinitionsFilePath { get; set; }

    [Description("Optional virtual host name for vhost-scoped definitions export")]
    public string? VirtualHostName { get; set; }
}
