using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.Redis;

namespace QaaS.Common.Probes.RedisProbes;

public class ExecuteRedisCommand : BaseRedisProbe<RedisCommandProbeConfig>
{
    protected override void RunRedisProbe()
    {
        var arguments = RedisCommandProbeSupport.BuildArguments(
            Context,
            Configuration.Arguments,
            null,
            false,
            Context.Logger,
            out _);

        Context.Logger.LogInformation(
            "Executing redis command {RedisCommand} with {ArgumentsCount} arguments on database {RedisDbNumber}",
            Configuration.Command,
            arguments.Length,
            Configuration.RedisDataBase);

        var result = RedisDb.Execute(Configuration.Command, arguments);
        if (!string.IsNullOrWhiteSpace(Configuration.StoreResultAs))
            RedisCommandProbeSupport.StoreResult(Context, Configuration.StoreResultAs, result);
    }
}
