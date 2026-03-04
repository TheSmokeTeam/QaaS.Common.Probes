using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.Redis;
using StackExchange.Redis;

namespace QaaS.Common.Probes.RedisProbes;

public class EmptyRedisByChunks<TEmptyRedisByChunksProbeConfig> : BaseRedisProbe<TEmptyRedisByChunksProbeConfig>
    where TEmptyRedisByChunksProbeConfig : RedisDataBaseBatchProbeConfig, new()
{
    protected override void RunRedisProbe()
    {
        Context.Logger.LogInformation("Emptying by chunks redis database {RedisDb}...", RedisDb);
        var cursor = 0L;
        do
        {
            RedisResult[]? result;
            try
            {
                result = (RedisResult[]?)RedisDb.Execute("SCAN", cursor.ToString(), "MATCH", "*", "COUNT",
                    Configuration.BatchSize.ToString());
            }
            catch (InvalidCastException)
            {
                result = null;
            }

            if (result is not { Length: 2 })
                throw new InvalidOperationException("Invalid response from redis database," +
                                                    $" received {result?.Length ?? 0} results, expected 2");
            cursor = (long)result[0];
            var keysToDelete = (RedisKey[])result[1]!;
            var deletedKeys = RedisDb.KeyDelete(keysToDelete);
            Context.Logger.LogDebug("Successfully deleted a chunk of {DeletedKeys}", deletedKeys);
        } while (cursor != 0);
    }
}
