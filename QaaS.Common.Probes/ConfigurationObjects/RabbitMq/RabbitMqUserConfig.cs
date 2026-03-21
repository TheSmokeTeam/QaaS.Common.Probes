using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

public record RabbitMqUserConfig
{
    [Required, Description("The rabbitmq user name")]
    public string? Username { get; set; }

    [Description("Optional password for the rabbitmq user")]
    public string? Password { get; set; }

    [Description("Optional password hash for the rabbitmq user")]
    public string? PasswordHash { get; set; }

    [Description("Optional hashing algorithm for the password hash")]
    public string? HashingAlgorithm { get; set; }

    [Description("Optional tags assigned to the rabbitmq user")]
    public string[]? Tags { get; set; }
}
