using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.Redis;
using StackExchange.Redis;
using System.Text.RegularExpressions;

namespace QaaS.Common.Probes.RedisProbes;

/// <summary>
/// Scans the selected Redis database in batches and deletes matching keys, optionally filtering them by a regular expression.
/// </summary>
public class EmptyRedisByChunks<TEmptyRedisByChunksProbeConfig> : BaseRedisProbeWithGlobalDict<TEmptyRedisByChunksProbeConfig>
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
            var keysToDelete = ((RedisKey[])result[1]!)
                .Where(ShouldDeleteKey)
                .ToArray();
            var deletedKeys = RedisDb.KeyDelete(keysToDelete);
            Context.Logger.LogDebug("Successfully deleted a chunk of {DeletedKeys}", deletedKeys);
        } while (cursor != 0);
    }

    private bool ShouldDeleteKey(RedisKey key)
    {
        return string.IsNullOrWhiteSpace(Configuration.KeyRegexPattern) ||
               Regex.IsMatch(key.ToString(), Configuration.KeyRegexPattern);
    }
}

/// <summary>
/// Concrete Redis chunk-deletion probe that uses the standard Redis batch probe configuration.
/// </summary>
/// <qaas-docs group="Redis maintenance" subgroup="Data cleanup" />
public class EmptyRedisByChunks : EmptyRedisByChunks<RedisDataBaseBatchProbeConfig>;
