using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

public abstract record BaseRabbitMqManagementConfig : BaseRabbitMqConfig
{
    [Description("Rabbitmq management API scheme"), DefaultValue("http")]
    public string ManagementScheme { get; set; } = "http";

    [Range(0, 65535), Description("Rabbitmq management API port"), DefaultValue(15672)]
    public int ManagementPort { get; set; } = 15672;

    [Description("Allow invalid TLS certificates when using HTTPS"), DefaultValue(false)]
    public bool AllowInvalidServerCertificates { get; set; }

    [Range(1, int.MaxValue), Description("Rabbitmq management API request timeout in milliseconds"),
     DefaultValue(30000)]
    public int RequestTimeoutMs { get; set; } = 30000;
}
