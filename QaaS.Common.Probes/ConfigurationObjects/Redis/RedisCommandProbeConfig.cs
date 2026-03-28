using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Redis;

public record RedisCommandProbeConfig : RedisDataBaseProbeBaseConfig
{
    [Required]
    [Description("Redis command to execute, for example SCAN, DEL or EVAL")]
    public string Command { get; set; } = string.Empty;

    [Description("Ordered list of command arguments")]
    public string[] Arguments { get; set; } = [];

    [Description("Optional key name used to store the command result for later commands or probes")]
    public string? StoreResultAs { get; set; }
}
