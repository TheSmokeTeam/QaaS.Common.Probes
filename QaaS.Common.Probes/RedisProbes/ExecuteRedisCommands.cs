using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.Redis;

namespace QaaS.Common.Probes.RedisProbes;

/// <summary>
/// Executes multiple Redis commands sequentially against the selected Redis database,
/// allowing later commands to reuse earlier results through redisResults placeholders.
/// </summary>
public class ExecuteRedisCommands : BaseRedisProbe<RedisExecuteCommandsConfig>
{
    protected override void RunRedisProbe()
    {
        foreach (var command in Configuration.Commands!)
        {
            var resolvedCommand = RedisCommandRuntimeResolver.ResolveCommand(Context, command.Command!);
            var resolvedArguments = RedisCommandRuntimeResolver.ResolveArguments(Context, command.Arguments);
            Context.Logger.LogDebug("Executing redis command {RedisCommand} on database {RedisDatabase}",
                resolvedCommand, Configuration.RedisDataBase);
            var result = RedisDb.Execute(resolvedCommand, resolvedArguments);
            RedisCommandRuntimeResolver.StoreResult(Context, command.StoreResultAs, result);
        }

        Context.Logger.LogInformation("Executed {RedisCommandCount} redis commands on database {RedisDatabase}",
            Configuration.Commands!.Length, Configuration.RedisDataBase);
    }
}
