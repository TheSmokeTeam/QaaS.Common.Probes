using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Redis;

public record RedisExecuteCommandsConfig : RedisDataBaseProbeBaseConfig
{
    [Required, MinLength(1), Description("The redis commands to execute")]
    public RedisCommandConfig[]? Commands { get; set; }
}
