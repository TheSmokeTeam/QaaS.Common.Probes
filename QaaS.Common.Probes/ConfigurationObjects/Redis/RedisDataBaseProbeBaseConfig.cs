using System.ComponentModel;
using QaaS.Common.Probes.ConfigurationObjects;
using QaaS.Framework.Protocols.ConfigurationObjects.Redis;

namespace QaaS.Common.Probes.ConfigurationObjects.Redis;

public record RedisDataBaseProbeBaseConfig : BaseRedisConfig, IUseGlobalDictProbeConfiguration
{
    [Description("When true, missing Redis probe configuration keys can be resolved from the shared global dictionary before local values are applied."),
     DefaultValue(false)]
    public bool UseGlobalDict { get; set; }

    [Description("Redis database to perform the probe on"), DefaultValue(0)]
    public int RedisDataBase { get; set; } = 0;
}
