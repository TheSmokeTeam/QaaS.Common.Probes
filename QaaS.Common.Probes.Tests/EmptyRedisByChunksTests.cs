using System.Reflection;
using Moq;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.Redis;
using QaaS.Common.Probes.RedisProbes;
using StackExchange.Redis;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class EmptyRedisByChunksTests
{
    [Test, TestCase(10, 100), TestCase(20, 100), TestCase(20, 200)]
    public void TestRunProbe_CallFunctionWithProbeWithXBatchSizeAndYKeys_KeyDeleteFunctionIsCalledYDividedByXTimes
        (int batchSize, int keysInRedis)
    {
        // Arrange
        var redisDataBaseMock = new Mock<IDatabase>();
        var callCount = 0;

        redisDataBaseMock.Setup(r =>
                r.Execute(It.IsAny<string>(), It.IsAny<string>(), "MATCH", "*", "COUNT",
                    It.IsAny<string>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount <= keysInRedis / batchSize)
                {
                    return RedisResult.Create([
                        RedisResult.Create(1L), RedisResult.Create([(callCount - 1).ToString()])
                    ]);
                }

                return RedisResult.Create([
                    RedisResult.Create(0L), RedisResult.Create(["last call"])
                ]);
            });
        redisDataBaseMock.SetupSequence(r => r.KeyDelete(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()));
        var probeConfig = new RedisDataBaseBatchProbeConfig()
        {
            BatchSize = batchSize
        };
        var probe = new EmptyRedisByChunks<RedisDataBaseBatchProbeConfig>();
        var configProperty = typeof(EmptyRedisByChunks<RedisDataBaseBatchProbeConfig>)
            .GetProperty("Configuration", BindingFlags.Public | BindingFlags.Instance);
        configProperty!.SetValue(probe, probeConfig);

        var contextProperty = typeof(EmptyRedisByChunks<RedisDataBaseBatchProbeConfig>)
            .GetProperty("Context", BindingFlags.Public | BindingFlags.Instance);
        contextProperty!.SetValue(probe, Globals.Context);

        var baseType = probe.GetType().BaseType;
        var dbField = baseType?.GetField("RedisDb", BindingFlags.NonPublic | BindingFlags.Instance);
        dbField!.SetValue(probe, redisDataBaseMock.Object);

        var runRedisProbeMethod = typeof(EmptyRedisByChunks<RedisDataBaseBatchProbeConfig>)
            .GetMethod("RunRedisProbe", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        runRedisProbeMethod!.Invoke(probe, null);

        // Assert
        redisDataBaseMock.Verify(m => m.KeyDelete(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()),
            Times.Exactly(keysInRedis / batchSize + 1));
    }

    [Test]
    public void TestRunProbe_WhenScanResponseIsInvalid_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var redisDataBaseMock = new Mock<IDatabase>();
        redisDataBaseMock.Setup(r =>
                r.Execute(It.IsAny<string>(), It.IsAny<string>(), "MATCH", "*", "COUNT",
                    It.IsAny<string>()))
            .Returns(RedisResult.Create([RedisResult.Create(0L)]));

        var probe = new EmptyRedisByChunks<RedisDataBaseBatchProbeConfig>
        {
            Configuration = new RedisDataBaseBatchProbeConfig { BatchSize = 10 },
            Context = Globals.Context
        };

        var baseType = probe.GetType().BaseType;
        var dbField = baseType?.GetField("RedisDb", BindingFlags.NonPublic | BindingFlags.Instance);
        dbField!.SetValue(probe, redisDataBaseMock.Object);

        var runRedisProbeMethod = typeof(EmptyRedisByChunks<RedisDataBaseBatchProbeConfig>)
            .GetMethod("RunRedisProbe", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act + Assert
        var ex = Assert.Throws<TargetInvocationException>(() => runRedisProbeMethod.Invoke(probe, null));
        Assert.That(ex!.InnerException, Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void TestRunProbe_WhenScanResponseIsScalar_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var redisDataBaseMock = new Mock<IDatabase>();
        redisDataBaseMock.Setup(r =>
                r.Execute(It.IsAny<string>(), It.IsAny<string>(), "MATCH", "*", "COUNT",
                    It.IsAny<string>()))
            .Returns(RedisResult.Create(42L));

        var probe = new EmptyRedisByChunks<RedisDataBaseBatchProbeConfig>
        {
            Configuration = new RedisDataBaseBatchProbeConfig { BatchSize = 10 },
            Context = Globals.Context
        };

        var baseType = probe.GetType().BaseType;
        var dbField = baseType?.GetField("RedisDb", BindingFlags.NonPublic | BindingFlags.Instance);
        dbField!.SetValue(probe, redisDataBaseMock.Object);

        var runRedisProbeMethod = typeof(EmptyRedisByChunks<RedisDataBaseBatchProbeConfig>)
            .GetMethod("RunRedisProbe", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act + Assert
        var ex = Assert.Throws<TargetInvocationException>(() => runRedisProbeMethod.Invoke(probe, null));
        Assert.That(ex!.InnerException, Is.TypeOf<InvalidOperationException>());
    }
}
