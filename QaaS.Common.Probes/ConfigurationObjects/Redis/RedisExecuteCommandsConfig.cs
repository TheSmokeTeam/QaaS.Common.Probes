using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Redis;

/// <summary>
/// Configuration for a probe that executes multiple Redis commands in sequence.
/// </summary>
public record RedisExecuteCommandsConfig : RedisDataBaseProbeBaseConfig
{
    /// <summary>
    /// Gets or sets the command sequence to execute.
    /// </summary>
    [Required, MinLength(1), Description("The redis commands to execute")]
    public RedisCommandConfig[]? Commands { get; set; }

    /// <summary>
    /// Gets or sets the optional repeat rule used for cursor-based command loops.
    /// </summary>
    [Description("Optional loop that repeats the command sequence until a stored redis result path matches the expected value")]
    public RedisCommandLoopConfig? RepeatUntil { get; set; }
}
