using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Redis;

public record RedisCommandStepConfig
{
    [Required]
    [Description("Unique name of the command step")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Description("Redis command to execute")]
    public string Command { get; set; } = string.Empty;

    [Description("Ordered list of command arguments. Supports placeholders like {{scan.0|0}}")]
    public string[] Arguments { get; set; } = [];

    [Description("Optional result path whose resolved value will be appended as extra command arguments")]
    public string? AppendArgumentsFromResultPath { get; set; }

    [Description("If true, skip the command when AppendArgumentsFromResultPath resolves to an empty array or null")]
    [DefaultValue(false)]
    public bool SkipWhenExpandedArgumentsEmpty { get; set; }

    [Description("Optional key name used to store the command result. Defaults to Name")]
    public string? StoreResultAs { get; set; }
}
