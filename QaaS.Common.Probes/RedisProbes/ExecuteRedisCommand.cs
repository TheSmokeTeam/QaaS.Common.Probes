using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.Redis;

namespace QaaS.Common.Probes.RedisProbes;

public class ExecuteRedisCommand : BaseRedisProbe<RedisExecuteCommandConfig>
{
    protected override void RunRedisProbe()
    {
        Context.Logger.LogInformation("Executing redis command {RedisCommand} on database {RedisDatabase}",
            Configuration.Command, Configuration.RedisDataBase);

        RedisDb.Execute(Configuration.Command!, BuildArguments(Configuration.Arguments));
    }

    private static object[] BuildArguments(IEnumerable<string>? arguments)
        => arguments?.Cast<object>().ToArray() ?? [];
}
