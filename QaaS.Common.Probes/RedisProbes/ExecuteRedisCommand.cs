using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.Redis;

namespace QaaS.Common.Probes.RedisProbes;

/// <summary>
/// Executes one Redis command with optional arguments against the selected Redis database,
/// optionally storing the result for later redisResults placeholder reuse.
/// </summary>
/// <qaas-docs group="Redis maintenance" subgroup="Command execution" />
public class ExecuteRedisCommand : BaseRedisProbeWithGlobalDictDefaults<RedisExecuteCommandConfig>
{
    /// <inheritdoc />
    protected override void RunRedisProbe()
    {
        var resolvedCommand = RedisCommandRuntimeResolver.ResolveCommand(Context, Configuration.Command!);
        var resolvedArguments = RedisCommandRuntimeResolver.ResolveArguments(Context, Configuration.Arguments);

        Context.Logger.LogInformation("Executing redis command {RedisCommand} on database {RedisDatabase}",
            resolvedCommand, Configuration.RedisDataBase);

        var result = RedisDb.Execute(resolvedCommand, resolvedArguments);
        RedisCommandRuntimeResolver.StoreResult(Context, Configuration.StoreResultAs, result);
    }
}
