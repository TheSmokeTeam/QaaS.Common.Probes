using System.Reflection;
using System.Text;
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
public class RedisExecuteProbesTests
{
    [Test]
    public void ExecuteRedisCommand_ShouldInvokeConfiguredCommandWithArguments()
    {
        var redisDbMock = new Mock<IDatabase>();
        redisDbMock.Setup(m => m.Execute(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns(RedisResult.Create(1L));

        var probe = new ExecuteRedisCommand
        {
            Configuration = new RedisExecuteCommandConfig
            {
                Command = "SET",
                Arguments = ["key-1", "value-1"]
            },
            Context = CreateContext()
        };
        SetRedisDbField(probe, redisDbMock.Object);

        InvokeRunRedisProbe(probe);

        redisDbMock.Verify(m => m.Execute("SET",
            It.Is<object[]>(arguments => arguments.Length == 2 &&
                                         (string)arguments[0] == "key-1" &&
                                         (string)arguments[1] == "value-1")), Times.Once);
    }

    [Test]
    public void ExecuteRedisCommand_WhenArgumentsAreNotProvided_ShouldInvokeCommandWithEmptyArgumentsArray()
    {
        var redisDbMock = new Mock<IDatabase>();
        redisDbMock.Setup(m => m.Execute(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns(RedisResult.Create(1L));

        var probe = new ExecuteRedisCommand
        {
            Configuration = new RedisExecuteCommandConfig
            {
                Command = "PING"
            },
            Context = CreateContext()
        };
        SetRedisDbField(probe, redisDbMock.Object);

        InvokeRunRedisProbe(probe);

        redisDbMock.Verify(m => m.Execute("PING",
            It.Is<object[]>(arguments => arguments.Length == 0)), Times.Once);
    }

    [Test]
    public void ExecuteRedisCommands_ShouldInvokeEveryConfiguredCommandInOrder()
    {
        var executedCommands = new List<(string Command, object[] Arguments)>();
        var redisDbMock = new Mock<IDatabase>();
        redisDbMock.Setup(m => m.Execute(It.IsAny<string>(), It.IsAny<object[]>()))
            .Callback<string, object[]>((command, arguments) => executedCommands.Add((command, arguments)))
            .Returns(RedisResult.Create(1L));

        var probe = new ExecuteRedisCommands
        {
            Configuration = new RedisExecuteCommandsConfig
            {
                Commands =
                [
                    new RedisCommandConfig
                    {
                        Command = "SET",
                        Arguments = ["key-1", "value-1"]
                    },
                    new RedisCommandConfig
                    {
                        Command = "DEL",
                        Arguments = ["key-1"]
                    }
                ]
            },
            Context = CreateContext()
        };
        SetRedisDbField(probe, redisDbMock.Object);

        InvokeRunRedisProbe(probe);

        Assert.That(executedCommands.Select(entry => entry.Command), Is.EqualTo(new[] { "SET", "DEL" }));
        Assert.That(executedCommands[0].Arguments.Select(argument => argument.ToString()),
            Is.EqualTo(new[] { "key-1", "value-1" }));
        Assert.That(executedCommands[1].Arguments.Select(argument => argument.ToString()),
            Is.EqualTo(new[] { "key-1" }));
    }

    [Test]
    public void ExecuteRedisCommands_WhenCommandHasNoArguments_ShouldExecuteItWithEmptyArgumentsArray()
    {
        var executedCommands = new List<(string Command, object[] Arguments)>();
        var redisDbMock = new Mock<IDatabase>();
        redisDbMock.Setup(m => m.Execute(It.IsAny<string>(), It.IsAny<object[]>()))
            .Callback<string, object[]>((command, arguments) => executedCommands.Add((command, arguments)))
            .Returns(RedisResult.Create(1L));

        var probe = new ExecuteRedisCommands
        {
            Configuration = new RedisExecuteCommandsConfig
            {
                Commands =
                [
                    new RedisCommandConfig
                    {
                        Command = "PING"
                    }
                ]
            },
            Context = CreateContext()
        };
        SetRedisDbField(probe, redisDbMock.Object);

        InvokeRunRedisProbe(probe);

        Assert.That(executedCommands, Has.Count.EqualTo(1));
        Assert.That(executedCommands[0].Command, Is.EqualTo("PING"));
        Assert.That(executedCommands[0].Arguments, Is.Empty);
    }

    [Test]
    public void ExecuteRedisCommand_WhenResultIsStored_ShouldAllowLaterProbeToReuseIt()
    {
        var executedCommands = new List<(string Command, object[] Arguments)>();
        var results = new Queue<RedisResult>([
            RedisResult.Create([
                RedisResult.Create((RedisValue)"0"),
                RedisResult.Create(new RedisValue[] { "key-1", "key-2" })
            ]),
            RedisResult.Create(2L)
        ]);
        var redisDbMock = new Mock<IDatabase>();
        redisDbMock.Setup(m => m.Execute(It.IsAny<string>(), It.IsAny<object[]>()))
            .Callback<string, object[]>((command, arguments) => executedCommands.Add((command, arguments)))
            .Returns(() => results.Dequeue());

        var context = CreateContext();
        var scanProbe = new ExecuteRedisCommand
        {
            Configuration = new RedisExecuteCommandConfig
            {
                Command = "SCAN",
                Arguments = ["0", "MATCH", "duplication:*", "COUNT", "1000"],
                StoreResultAs = "scanResult"
            },
            Context = context
        };

        var deleteProbe = new ExecuteRedisCommand
        {
            Configuration = new RedisExecuteCommandConfig
            {
                Command = "DEL",
                Arguments = ["${redisResults:scanResult:1}"]
            },
            Context = context
        };

        SetRedisDbField(scanProbe, redisDbMock.Object);
        SetRedisDbField(deleteProbe, redisDbMock.Object);

        InvokeRunRedisProbe(scanProbe);
        InvokeRunRedisProbe(deleteProbe);

        Assert.That(executedCommands.Select(entry => entry.Command), Is.EqualTo(new[] { "SCAN", "DEL" }));
        Assert.That(executedCommands[1].Arguments.Select(FormatArgument),
            Is.EqualTo(new[] { "key-1", "key-2" }));
    }

    [Test]
    public void ExecuteRedisCommands_WhenStoredResultReferencedByLaterCommand_ShouldExpandArguments()
    {
        var executedCommands = new List<(string Command, object[] Arguments)>();
        var results = new Queue<RedisResult>([
            RedisResult.Create([
                RedisResult.Create((RedisValue)"0"),
                RedisResult.Create(new RedisValue[] { "key-1", "key-2" })
            ]),
            RedisResult.Create(2L)
        ]);
        var redisDbMock = new Mock<IDatabase>();
        redisDbMock.Setup(m => m.Execute(It.IsAny<string>(), It.IsAny<object[]>()))
            .Callback<string, object[]>((command, arguments) => executedCommands.Add((command, arguments)))
            .Returns(() => results.Dequeue());

        var probe = new ExecuteRedisCommands
        {
            Configuration = new RedisExecuteCommandsConfig
            {
                Commands =
                [
                    new RedisCommandConfig
                    {
                        Command = "SCAN",
                        Arguments = ["0", "MATCH", "duplication:*", "COUNT", "1000"],
                        StoreResultAs = "scanResult"
                    },
                    new RedisCommandConfig
                    {
                        Command = "DEL",
                        Arguments = ["${redisResults:scanResult:1}"]
                    }
                ]
            },
            Context = CreateContext()
        };

        SetRedisDbField(probe, redisDbMock.Object);

        InvokeRunRedisProbe(probe);

        Assert.That(executedCommands.Select(entry => entry.Command), Is.EqualTo(new[] { "SCAN", "DEL" }));
        Assert.That(executedCommands[1].Arguments.Select(FormatArgument),
            Is.EqualTo(new[] { "key-1", "key-2" }));
    }

    [Test]
    public void ExecuteRedisCommands_WhenRepeatUntilIsConfigured_ShouldReuseCursorAcrossIterations()
    {
        var executedCommands = new List<(string Command, object[] Arguments)>();
        var results = new Queue<RedisResult>([
            RedisResult.Create([
                RedisResult.Create((RedisValue)"7"),
                RedisResult.Create(new RedisValue[] { "key-1", "key-2" })
            ]),
            RedisResult.Create(2L),
            RedisResult.Create([
                RedisResult.Create((RedisValue)"0"),
                RedisResult.Create(new RedisValue[] { "key-3" })
            ]),
            RedisResult.Create(1L)
        ]);

        var redisDbMock = new Mock<IDatabase>();
        redisDbMock.Setup(m => m.Execute(It.IsAny<string>(), It.IsAny<object[]>()))
            .Callback<string, object[]>((command, arguments) => executedCommands.Add((command, arguments)))
            .Returns(() => results.Dequeue());

        var probe = new ExecuteRedisCommands
        {
            Configuration = new RedisExecuteCommandsConfig
            {
                Commands =
                [
                    new RedisCommandConfig
                    {
                        Command = "SCAN",
                        Arguments = ["${redisResults:scanResult:0??0}", "MATCH", "duplication:*", "COUNT", "1000"],
                        StoreResultAs = "scanResult"
                    },
                    new RedisCommandConfig
                    {
                        Command = "DEL",
                        Arguments = ["${redisResults:scanResult:1}"]
                    }
                ],
                RepeatUntil = new RedisCommandLoopConfig
                {
                    ResultPath = "scanResult:0",
                    ExpectedValue = "0",
                    MaxIterations = 10
                }
            },
            Context = CreateContext()
        };

        SetRedisDbField(probe, redisDbMock.Object);

        InvokeRunRedisProbe(probe);

        Assert.That(executedCommands.Select(entry => entry.Command),
            Is.EqualTo(new[] { "SCAN", "DEL", "SCAN", "DEL" }));
        Assert.That(executedCommands[0].Arguments.Select(FormatArgument),
            Is.EqualTo(new[] { "0", "MATCH", "duplication:*", "COUNT", "1000" }));
        Assert.That(executedCommands[1].Arguments.Select(FormatArgument),
            Is.EqualTo(new[] { "key-1", "key-2" }));
        Assert.That(executedCommands[2].Arguments.Select(FormatArgument),
            Is.EqualTo(new[] { "7", "MATCH", "duplication:*", "COUNT", "1000" }));
        Assert.That(executedCommands[3].Arguments.Select(FormatArgument),
            Is.EqualTo(new[] { "key-3" }));
    }

    [Test]
    public void ExecuteRedisCommands_WhenSkippedCommandStoredResultWouldBeReused_ShouldClearIt()
    {
        var executedCommands = new List<(string Command, object[] Arguments)>();
        var results = new Queue<RedisResult>([
            RedisResult.Create([
                RedisResult.Create((RedisValue)"7"),
                RedisResult.Create(new RedisValue[] { "key-1" })
            ]),
            RedisResult.Create(1L),
            RedisResult.Create((RedisValue)"deleted"),
            RedisResult.Create([
                RedisResult.Create((RedisValue)"0"),
                RedisResult.Create(Array.Empty<RedisValue>())
            ]),
            RedisResult.Create((RedisValue)"stale")
        ]);

        var redisDbMock = new Mock<IDatabase>();
        redisDbMock.Setup(m => m.Execute(It.IsAny<string>(), It.IsAny<object[]>()))
            .Callback<string, object[]>((command, arguments) => executedCommands.Add((command, arguments)))
            .Returns(() => results.Dequeue());

        var probe = new ExecuteRedisCommands
        {
            Configuration = new RedisExecuteCommandsConfig
            {
                Commands =
                [
                    new RedisCommandConfig
                    {
                        Command = "SCAN",
                        Arguments = ["${redisResults:scanResult:0??0}", "MATCH", "duplication:*", "COUNT", "1000"],
                        StoreResultAs = "scanResult"
                    },
                    new RedisCommandConfig
                    {
                        Command = "DEL",
                        Arguments = ["${redisResults:scanResult:1}"],
                        StoreResultAs = "deleteResult"
                    },
                    new RedisCommandConfig
                    {
                        Command = "ECHO",
                        Arguments = ["${redisResults:deleteResult}"]
                    }
                ],
                RepeatUntil = new RedisCommandLoopConfig
                {
                    ResultPath = "scanResult:0",
                    ExpectedValue = "0",
                    MaxIterations = 10
                }
            },
            Context = CreateContext()
        };

        SetRedisDbField(probe, redisDbMock.Object);

        InvokeRunRedisProbe(probe);

        Assert.That(executedCommands.Select(entry => entry.Command),
            Is.EqualTo(new[] { "SCAN", "DEL", "ECHO", "SCAN" }));
        Assert.That(executedCommands[2].Arguments.Select(FormatArgument),
            Is.EqualTo(new[] { "1" }));
    }

    private static void InvokeRunRedisProbe(object probe)
    {
        var method = probe.GetType().GetMethod("RunRedisProbe", BindingFlags.NonPublic | BindingFlags.Instance)!;
        method.Invoke(probe, null);
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
            InternalRunningSessions = new RunningSessions(new Dictionary<string, RunningSessionData<object, object>>())
        };
    }

    private static string? FormatArgument(object? argument)
    {
        return argument switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            _ => argument?.ToString()
        };
    }
}
