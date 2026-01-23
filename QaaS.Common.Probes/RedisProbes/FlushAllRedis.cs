using Microsoft.Extensions.Logging;
using QaaS.Framework.Protocols.ConfigurationObjects.Redis;

namespace QaaS.Common.Probes.RedisProbes;

public class FlushAllRedis : BaseRedisProbe<BaseRedisConfig>
{
    private const string FlushAllCommand = "FLUSHALL";

    protected override void RunRedisProbe()
    {
        Context.Logger.LogInformation("Running {FlushAllCommand} probe on redis servers: {RedisEndPoints}",
            FlushAllCommand, string.Join(", ", Configuration.HostNames!));
        RedisDb.Execute(FlushAllCommand);
        Context.Logger.LogInformation("Finished flushing all the redis servers' data successfully");
    }
}