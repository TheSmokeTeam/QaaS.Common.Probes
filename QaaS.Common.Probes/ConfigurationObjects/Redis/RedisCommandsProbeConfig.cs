using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Redis;

public record RedisCommandsProbeConfig : RedisDataBaseProbeBaseConfig
{
    [Required]
    [MinLength(1)]
    [Description("Ordered Redis commands to execute")]
    public RedisCommandStepConfig[] Commands { get; set; } = [];

    [Description("Optional repeat loop for the full command sequence")]
    public RedisCommandsLoopConfig? RepeatUntil { get; set; }
}
