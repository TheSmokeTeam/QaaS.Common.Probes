using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.Redis;

namespace QaaS.Common.Probes.RedisProbes;

/// <summary>
/// Executes multiple Redis commands sequentially against the selected Redis database.
/// </summary>
public class ExecuteRedisCommands : BaseRedisProbe<RedisExecuteCommandsConfig>
{
    protected override void RunRedisProbe()
    {
        foreach (var command in Configuration.Commands!)
        {
            Context.Logger.LogDebug("Executing redis command {RedisCommand} on database {RedisDatabase}",
                command.Command, Configuration.RedisDataBase);
            RedisDb.Execute(command.Command!, BuildArguments(command.Arguments));
        }

        Context.Logger.LogInformation("Executed {RedisCommandCount} redis commands on database {RedisDatabase}",
            Configuration.Commands!.Length, Configuration.RedisDataBase);
    }

    private static object[] BuildArguments(IEnumerable<string>? arguments)
        => arguments?.Cast<object>().ToArray() ?? [];
}
