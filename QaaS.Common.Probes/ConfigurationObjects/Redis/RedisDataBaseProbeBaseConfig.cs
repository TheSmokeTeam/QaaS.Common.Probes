using System.ComponentModel;
using QaaS.Framework.Protocols.ConfigurationObjects.Redis;

namespace QaaS.Common.Probes.ConfigurationObjects.Redis;

public record RedisDataBaseProbeBaseConfig : BaseRedisConfig
{
    [Description("Redis database to perform the probe on"), DefaultValue(0)]
    public int RedisDataBase { get; set; } = 0;
}