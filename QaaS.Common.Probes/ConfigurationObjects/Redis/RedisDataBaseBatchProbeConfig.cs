using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Redis;

public record RedisDataBaseBatchProbeConfig : RedisDataBaseProbeBaseConfig
{
    [Description("Batch Size to do the operation on"), Range(0, int.MaxValue), DefaultValue(100)]
    public int BatchSize { get; set; } = 100;
}