using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Redis;

/// <summary>
/// Describes one Redis command step inside a multi-command execute probe.
/// </summary>
public record RedisCommandConfig
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
