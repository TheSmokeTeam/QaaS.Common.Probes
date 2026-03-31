using System.Reflection;
using Moq;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.Redis;
using QaaS.Common.Probes.RedisProbes;
using QaaS.Framework.Protocols.ConfigurationObjects.Redis;
using StackExchange.Redis;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class FlushRedisProbesTests
{
    [Test]
    public void TestFlushDbRedisRunRedisProbe_ShouldExecuteFlushDbCommand()
    {
        // Arrange
        var redisDbMock = new Mock<IDatabase>();
        redisDbMock.Setup(m => m.Execute(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns(RedisResult.Create(1L));

        var probe = new FlushDbRedis
        {
            Configuration = new RedisDataBaseProbeBaseConfig(),
            Context = Globals.Context
        };
        SetHostNames(probe.Configuration, "localhost");
        SetRedisDbField(probe, redisDbMock.Object);

        var runRedisProbeMethod = typeof(FlushDbRedis)
            .GetMethod("RunRedisProbe", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        runRedisProbeMethod.Invoke(probe, null);

        // Assert
        redisDbMock.Verify(m => m.Execute("FLUSHDB", It.IsAny<object[]>()), Times.Once);
    }

    [Test]
    public void TestFlushAllRedisRunRedisProbe_ShouldExecuteFlushAllCommand()
    {
        // Arrange
        var redisDbMock = new Mock<IDatabase>();
        redisDbMock.Setup(m => m.Execute(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns(RedisResult.Create(1L));

        var probe = new FlushAllRedis
        {
            Configuration = new RedisServerProbeConfig(),
            Context = Globals.Context
        };
        SetHostNames(probe.Configuration, "localhost");
        SetRedisDbField(probe, redisDbMock.Object);

        var runRedisProbeMethod = typeof(FlushAllRedis)
            .GetMethod("RunRedisProbe", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        runRedisProbeMethod.Invoke(probe, null);

        // Assert
        redisDbMock.Verify(m => m.Execute("FLUSHALL", It.IsAny<object[]>()), Times.Once);
    }

    private static void SetRedisDbField(object probe, IDatabase redisDb)
    {
        var baseType = probe.GetType().BaseType;
        var redisDbField = baseType?.GetField("RedisDb", BindingFlags.NonPublic | BindingFlags.Instance);
        redisDbField!.SetValue(probe, redisDb);
    }

    private static void SetHostNames(BaseRedisConfig config, params string[] hostNames)
    {
        var hostNamesProperty = typeof(BaseRedisConfig).GetProperty("HostNames")!;
        var hostNamesType = hostNamesProperty.PropertyType;

        object value = hostNamesType == typeof(string[]) ? hostNames : hostNames.ToList();
        hostNamesProperty.SetValue(config, value);
    }
}
