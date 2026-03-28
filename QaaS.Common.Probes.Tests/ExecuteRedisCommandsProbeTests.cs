using System.Reflection;
using Moq;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.Redis;
using QaaS.Common.Probes.RedisProbes;
using QaaS.Framework.SDK.ContextObjects;
using QaaS.Framework.SDK.Session.SessionDataObjects;
using QaaS.Framework.SDK.Session.SessionDataObjects.RunningSessionsObjects;
using StackExchange.Redis;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class ExecuteRedisCommandsProbeTests
{
    [Test]
    public void ExecuteRedisCommand_WhenStoreResultAsIsConfigured_ShouldStoreResultInContext()
    {
        var redisDbMock = new Mock<IDatabase>();
        redisDbMock.Setup(database => database.Execute("PING", It.IsAny<object[]>()))
            .Returns(RedisResult.Create((RedisValue)"PONG"));

        var probe = new ExecuteRedisCommand
        {
            Configuration = new RedisCommandProbeConfig
            {
                Command = "PING",
                StoreResultAs = "ping"
            },
            Context = CreateContext()
        };
        SetRedisDbField(probe, redisDbMock.Object);

        InvokeRunRedisProbe(probe);

        Assert.That(
            probe.Context.GetValueFromGlobalDictionary(["RedisCommandResults", "ping"])?.ToString(),
            Is.EqualTo("PONG"));
    }

    [Test]
    public void ExecuteRedisCommands_WhenUsingStoredScanResult_ShouldLoopAndAppendKeysToDelCommand()
    {
        var redisDbMock = new Mock<IDatabase>();
        var capturedCalls = new List<(string Command, object[] Arguments)>();

        redisDbMock.Setup(database => database.Execute(It.IsAny<string>(), It.IsAny<object[]>()))
            .Callback<string, object[]>((command, arguments) =>
                capturedCalls.Add((command, arguments)))
            .Returns<string, object[]>((command, _) =>
            {
                return command switch
                {
                    "SCAN" when capturedCalls.Count(call => call.Command == "SCAN") == 1 => RedisResult.Create([
                        RedisResult.Create((RedisValue)"7"),
                        RedisResult.Create(new RedisResult[]
                        {
                            RedisResult.Create((RedisValue)"duplication:1"),
                            RedisResult.Create((RedisValue)"duplication:2")
                        })
                    ]),
                    "SCAN" => RedisResult.Create([
                        RedisResult.Create((RedisValue)"0"),
                        RedisResult.Create(Array.Empty<RedisResult>())
                    ]),
                    "DEL" => RedisResult.Create(2L),
                    _ => throw new InvalidOperationException($"Unexpected command {command}")
                };
            });

        var probe = new ExecuteRedisCommands
        {
            Configuration = new RedisCommandsProbeConfig
            {
                Commands =
                [
                    new RedisCommandStepConfig
                    {
                        Name = "scan",
                        Command = "SCAN",
                        Arguments = ["{{scan.0|0}}", "MATCH", "duplication:*", "COUNT", "1000"]
                    },
                    new RedisCommandStepConfig
                    {
                        Name = "delete",
                        Command = "DEL",
                        AppendArgumentsFromResultPath = "scan.1",
                        SkipWhenExpandedArgumentsEmpty = true
                    }
                ],
                RepeatUntil = new RedisCommandsLoopConfig
                {
                    ResultPath = "scan.0",
                    ExpectedValue = "0",
                    MaxIterations = 5
                }
            },
            Context = CreateContext()
        };
        SetRedisDbField(probe, redisDbMock.Object);

        InvokeRunRedisProbe(probe);

        Assert.That(capturedCalls.Count, Is.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(capturedCalls[0].Command, Is.EqualTo("SCAN"));
            Assert.That(capturedCalls[0].Arguments, Is.EqualTo(new object[] { "0", "MATCH", "duplication:*", "COUNT", "1000" }));
            Assert.That(capturedCalls[1].Command, Is.EqualTo("DEL"));
            Assert.That(capturedCalls[1].Arguments, Is.EqualTo(new object[] { "duplication:1", "duplication:2" }));
            Assert.That(capturedCalls[2].Command, Is.EqualTo("SCAN"));
            Assert.That(capturedCalls[2].Arguments, Is.EqualTo(new object[] { "7", "MATCH", "duplication:*", "COUNT", "1000" }));
        });
    }

    private static void InvokeRunRedisProbe(object probe)
    {
        var runRedisProbeMethod = probe.GetType()
            .GetMethod("RunRedisProbe", BindingFlags.NonPublic | BindingFlags.Instance)!;
        runRedisProbeMethod.Invoke(probe, null);
    }

    private static void SetRedisDbField(object probe, IDatabase redisDb)
    {
        var baseType = probe.GetType().BaseType;
        var redisDbField = baseType?.GetField("RedisDb", BindingFlags.NonPublic | BindingFlags.Instance);
        redisDbField!.SetValue(probe, redisDb);
    }

    private static InternalContext CreateContext()
    {
        return new InternalContext
        {
            Logger = Globals.Logger,
            InternalRunningSessions =
                new RunningSessions(new Dictionary<string, RunningSessionData<object, object>>())
        };
    }
}
