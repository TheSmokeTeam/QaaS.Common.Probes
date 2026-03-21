using System.Reflection;
using Moq;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.Redis;
using QaaS.Common.Probes.RedisProbes;
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
            Context = Globals.Context
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
            Context = Globals.Context
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
            Context = Globals.Context
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
            Context = Globals.Context
        };
        SetRedisDbField(probe, redisDbMock.Object);

        InvokeRunRedisProbe(probe);

        Assert.That(executedCommands, Has.Count.EqualTo(1));
        Assert.That(executedCommands[0].Command, Is.EqualTo("PING"));
        Assert.That(executedCommands[0].Arguments, Is.Empty);
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
}
