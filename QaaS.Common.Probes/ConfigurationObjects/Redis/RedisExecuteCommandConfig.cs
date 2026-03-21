using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Redis;

public record RedisExecuteCommandConfig : RedisDataBaseProbeBaseConfig
{
    [Required, Description("The redis command to execute")]
    public string? Command { get; set; }

    [Description("Optional redis command arguments")]
    public string[]? Arguments { get; set; }
}
