using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Redis;

/// <summary>
/// Configuration for a probe that executes a single Redis command.
/// </summary>
public record RedisExecuteCommandConfig : RedisDataBaseProbeBaseConfig
{
    /// <summary>
    /// Gets or sets the Redis command name to execute.
    /// </summary>
    [Required, Description("The redis command to execute")]
    public string? Command { get; set; }

    /// <summary>
    /// Gets or sets the optional command arguments.
    /// </summary>
    [Description("Optional redis command arguments")]
    public string[]? Arguments { get; set; }

    /// <summary>
    /// Gets or sets the optional alias used to store the command result for later reuse.
    /// </summary>
    [Description("Optional alias used to store the command result for later redisResults placeholders")]
    public string? StoreResultAs { get; set; }
}
