using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Redis;

public record RedisCommandsLoopConfig
{
    [Required]
    [Description("Stored result path checked after each full command sequence, for example scan.0")]
    public string ResultPath { get; set; } = string.Empty;

    [Description("Stop repeating when the resolved value equals this string")]
    [DefaultValue("0")]
    public string ExpectedValue { get; set; } = "0";

    [Range(1, int.MaxValue)]
    [Description("Safety limit for the number of command sequence iterations")]
    [DefaultValue(1000)]
    public int MaxIterations { get; set; } = 1000;
}
