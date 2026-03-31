using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.Redis;

namespace QaaS.Common.Probes.RedisProbes;

/// <summary>
/// Runs Redis FLUSHDB against the selected Redis database.
/// </summary>
/// <qaas-docs group="Redis maintenance" subgroup="Database flush" />
public class FlushDbRedis : BaseRedisProbeWithGlobalDict<RedisDataBaseProbeBaseConfig>
{
    private const string FlushDbCommand = "FLUSHDB";

    protected override void RunRedisProbe()
    {
        Context.Logger.LogInformation(
            "Running {FlushDbCommand} probe on redis servers: {RedisEndPoints}, database number {RedisDbNumber}",
            FlushDbCommand, string.Join(", ", Configuration.HostNames!), Configuration.RedisDataBase.ToString());
        RedisDb.Execute(FlushDbCommand);
        Context.Logger.LogInformation("Finished flushing specific db in the redis servers' successfully");
    }
}
