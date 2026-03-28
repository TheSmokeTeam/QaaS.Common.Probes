using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Redis;

/// <summary>
/// Defines when a multi-command Redis probe should repeat its command sequence.
/// </summary>
public record RedisCommandLoopConfig
{
    /// <summary>
    /// Gets or sets the stored <c>redisResults</c> path to inspect after each iteration.
    /// </summary>
    [Required, Description("Stored redisResults path to inspect after each command sequence iteration")]
    public string? ResultPath { get; set; }

    /// <summary>
    /// Gets or sets the scalar value that stops the loop when the resolved path matches it.
    /// </summary>
    [Required, Description("Scalar value that ends the loop when the resolved ResultPath matches it")]
    public string? ExpectedValue { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of iterations allowed before the probe fails.
    /// </summary>
    [Range(1, int.MaxValue), Description("Safety cap for loop iterations"), DefaultValue(100)]
    public int MaxIterations { get; set; } = 100;
}
