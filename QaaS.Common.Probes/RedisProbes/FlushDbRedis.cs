using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.Redis;

namespace QaaS.Common.Probes.RedisProbes;

public class FlushDbRedis : BaseRedisProbe<RedisDataBaseProbeBaseConfig>
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