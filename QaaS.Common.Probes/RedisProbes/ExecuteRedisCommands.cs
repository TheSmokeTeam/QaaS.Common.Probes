using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.Redis;

namespace QaaS.Common.Probes.RedisProbes;

public class ExecuteRedisCommands : BaseRedisProbe<RedisCommandsProbeConfig>
{
    protected override void RunRedisProbe()
    {
        if (Configuration.RepeatUntil is null)
        {
            ExecuteCommandsOnce();
            return;
        }

        for (var iteration = 1; iteration <= Configuration.RepeatUntil.MaxIterations; iteration++)
        {
            ExecuteCommandsOnce();

            var resolvedValue = RedisCommandProbeSupport.ResolveValueAsString(
                Context,
                Configuration.RepeatUntil.ResultPath);
            if (string.Equals(resolvedValue, Configuration.RepeatUntil.ExpectedValue, StringComparison.Ordinal))
            {
                Context.Logger.LogInformation(
                    "Finished redis command sequence after {IterationCount} iteration(s); repeat condition {ResultPath} == {ExpectedValue} was met",
                    iteration,
                    Configuration.RepeatUntil.ResultPath,
                    Configuration.RepeatUntil.ExpectedValue);
                return;
            }
        }

        throw new InvalidOperationException(
            $"Redis command sequence did not meet repeat condition '{Configuration.RepeatUntil.ResultPath} == {Configuration.RepeatUntil.ExpectedValue}' within {Configuration.RepeatUntil.MaxIterations} iterations.");
    }

    private void ExecuteCommandsOnce()
    {
        foreach (var commandStep in Configuration.Commands)
        {
            var arguments = RedisCommandProbeSupport.BuildArguments(
                Context,
                commandStep.Arguments,
                commandStep.AppendArgumentsFromResultPath,
                commandStep.SkipWhenExpandedArgumentsEmpty,
                Context.Logger,
                out var shouldSkip);
            if (shouldSkip)
                continue;

            Context.Logger.LogInformation(
                "Executing redis command step {CommandStepName} ({RedisCommand}) with {ArgumentsCount} arguments on database {RedisDbNumber}",
                commandStep.Name,
                commandStep.Command,
                arguments.Length,
                Configuration.RedisDataBase);

            var result = RedisDb.Execute(commandStep.Command, arguments);
            var resultKey = string.IsNullOrWhiteSpace(commandStep.StoreResultAs)
                ? commandStep.Name
                : commandStep.StoreResultAs;
            RedisCommandProbeSupport.StoreResult(Context, resultKey!, result);
        }
    }
}
