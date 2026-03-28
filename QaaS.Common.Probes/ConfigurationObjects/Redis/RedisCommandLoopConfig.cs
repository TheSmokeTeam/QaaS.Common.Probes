using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Redis;

public record RedisCommandLoopConfig
{
    [Required, Description("Stored redisResults path to inspect after each command sequence iteration")]
    public string? ResultPath { get; set; }

    [Required, Description("Scalar value that ends the loop when the resolved ResultPath matches it")]
    public string? ExpectedValue { get; set; }

    [Range(1, int.MaxValue), Description("Safety cap for loop iterations"), DefaultValue(100)]
    public int MaxIterations { get; set; } = 100;
}
