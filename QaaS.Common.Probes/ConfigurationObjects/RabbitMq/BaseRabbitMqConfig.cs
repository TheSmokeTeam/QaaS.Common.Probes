using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using QaaS.Common.Probes.ConfigurationObjects;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

/// <summary>
/// Base rabbitmq configurations relevant to any action related to rabbitmq
/// </summary>
public abstract record BaseRabbitMqConfig : IUseGlobalDictProbeConfiguration
{
    [Description("When true, missing RabbitMQ probe configuration keys can be resolved from the shared global dictionary before local values are applied."),
     DefaultValue(false)]
    public bool UseGlobalDict { get; set; }

    [Required, Description("Rabbitmq hostname")]
    public string? Host { get; set; }

    [Description("Rabbitmq username"), DefaultValue("admin")]
    public string Username { get; set; } = "admin";

    [Description("Rabbitmq password"), DefaultValue("admin")]
    public string Password { get; set; } = "admin";

    [Range(0, 65535), Description("Rabbitmq Amqp port"), DefaultValue(5672)]
    public int Port { get; set; } = 5672;

    [Description("Rabbitmq virtual host to access during this connection"), DefaultValue("/")]
    public string VirtualHost { get; set; } = "/";
}
