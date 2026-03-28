using Microsoft.Extensions.Logging;
using QaaS.Framework.Protocols.ConfigurationObjects.Redis;

namespace QaaS.Common.Probes.RedisProbes;

/// <summary>
/// Runs Redis FLUSHALL against the configured server to remove keys from every database.
/// </summary>
/// <qaas-docs group="Redis maintenance" subgroup="Database flush" />
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
