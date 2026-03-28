using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.Redis;

namespace QaaS.Common.Probes.RedisProbes;

/// <summary>
/// Executes multiple Redis commands sequentially against the selected Redis database,
/// allowing later commands to reuse earlier results through redisResults placeholders and optional looping.
/// </summary>
/// <qaas-docs group="Redis maintenance" subgroup="Command execution" />
public class ExecuteRedisCommands : BaseRedisProbe<RedisExecuteCommandsConfig>
{
    /// <inheritdoc />
    protected override void RunRedisProbe()
    {
        var iteration = 0;
        do
        {
            iteration++;
            foreach (var command in Configuration.Commands!)
            {
                var resolvedCommand = RedisCommandRuntimeResolver.ResolveCommand(Context, command.Command!);
                var resolvedArguments = RedisCommandRuntimeResolver.ResolveArguments(Context, command.Arguments);
                if (command.Arguments is { Length: > 0 } && resolvedArguments.Length == 0)
                {
                    Context.Logger.LogDebug(
                        "Skipping redis command {RedisCommand} on database {RedisDatabase} because all arguments resolved to an empty collection",
                        resolvedCommand, Configuration.RedisDataBase);
                    RedisCommandRuntimeResolver.StoreEmptyResult(Context, command.StoreResultAs);
                    continue;
                }

                Context.Logger.LogDebug("Executing redis command {RedisCommand} on database {RedisDatabase}",
                    resolvedCommand, Configuration.RedisDataBase);
                var result = RedisDb.Execute(resolvedCommand, resolvedArguments);
                RedisCommandRuntimeResolver.StoreResult(Context, command.StoreResultAs, result);
            }
        }
        while (ShouldRepeat(iteration));

        Context.Logger.LogInformation("Executed {RedisCommandCount} redis commands on database {RedisDatabase}",
            Configuration.Commands!.Length, Configuration.RedisDataBase);
    }

    /// <summary>
    /// Evaluates the configured loop condition after one full command-sequence iteration.
    /// </summary>
    /// <param name="iteration">The 1-based iteration number that just completed.</param>
    /// <returns><see langword="true"/> when another iteration is required; otherwise <see langword="false"/>.</returns>
    private bool ShouldRepeat(int iteration)
    {
        if (Configuration.RepeatUntil == null)
            return false;

        if (iteration >= Configuration.RepeatUntil.MaxIterations)
        {
            throw new InvalidOperationException(
                $"Redis command loop exceeded the configured maximum of {Configuration.RepeatUntil.MaxIterations} iterations.");
        }

        var currentValue = RedisCommandRuntimeResolver.ResolveStoredResultAsString(
            Context,
            Configuration.RepeatUntil.ResultPath!);

        return !string.Equals(currentValue, Configuration.RepeatUntil.ExpectedValue, StringComparison.Ordinal);
    }
}
