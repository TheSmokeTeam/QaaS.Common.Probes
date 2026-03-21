using System.ComponentModel;

namespace QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

public record RabbitMqPermissionConfig : RabbitMqPermissionTargetConfig
{
    [Description("Regex for configure permissions"), DefaultValue(".*")]
    public string Configure { get; set; } = ".*";

    [Description("Regex for write permissions"), DefaultValue(".*")]
    public string Write { get; set; } = ".*";

    [Description("Regex for read permissions"), DefaultValue(".*")]
    public string Read { get; set; } = ".*";
}
