using System.Reflection;
using Amazon.S3;
using k8s;
using Moq;
using Nest;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.Elastic;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Common.Probes.ConfigurationObjects.Redis;
using QaaS.Common.Probes.ConfigurationObjects.S3;
using QaaS.Common.Probes.ElasticProbes;
using QaaS.Common.Probes.OsProbes;
using QaaS.Common.Probes.RabbitMqProbes;
using QaaS.Common.Probes.RedisProbes;
using QaaS.Common.Probes.S3Probes;
using QaaS.Framework.Protocols.ConfigurationObjects.Redis;
using QaaS.Framework.Protocols.Utils.S3Utils;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class BaseProbeCoverageTests
{
    private sealed class TestableElasticProbe : BaseElasticProbe<EmptyElasticIndicesConfig>
    {
        public bool RunElasticProbeCalled { get; private set; }
        public IElasticClient? ObservedClient { get; private set; }

        protected override void RunElasticProbe()
        {
            RunElasticProbeCalled = true;
            ObservedClient = ElasticClient;
        }
    }

    private sealed class TestableRedisProbe(IConnectionMultiplexer connectionMultiplexer)
        : BaseRedisProbe<RedisDataBaseBatchProbeConfig>
    {
        public bool RunRedisProbeCalled { get; private set; }
        public IDatabase? ObservedDatabase { get; private set; }

        protected override IConnectionMultiplexer CreateConnectionMultiplexer(ConfigurationOptions configurationOptions,
            TextWriter consoleWriter)
            => connectionMultiplexer;

        protected override void RunRedisProbe()
        {
            RunRedisProbeCalled = true;
            ObservedDatabase = RedisDb;
        }
    }

    private sealed class TestableOsProbe(Kubernetes kubernetesClient) : BaseOsProbe<OsProbeConfig>
    {
        public bool RunOsProbeCalled { get; private set; }

        protected override Kubernetes CreateConnection() => kubernetesClient;

        protected override void RunOsProbe()
        {
            RunOsProbeCalled = true;
        }
    }

    private sealed class TestableS3Probe : BaseS3Probe<EmptyS3BucketConfig>
    {
        public bool RunS3ProbeCalled { get; private set; }
        public IAmazonS3? ObservedS3Client { get; private set; }
        public IS3Client? ObservedTransferClient { get; private set; }

        protected override void RunS3Probe()
        {
            RunS3ProbeCalled = true;
            ObservedS3Client = S3Client;
            ObservedTransferClient = DataTransferS3Client;
        }
    }

    private sealed class TestableRabbitMqRunProbe
        : BaseRabbitMqObjectsManipulation<DeleteRabbitMqQueuesConfig, string>
    {
        private readonly IConnectionFactory _connectionFactory;

        public TestableRabbitMqRunProbe(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public List<string> ManipulatedObjects { get; } = [];

        protected override IConnectionFactory GetRabbitConnection()
        {
            RabbitmqConnectionString = "amqp://user:pass@rabbit-host:5678";
            return _connectionFactory;
        }

        protected override IEnumerable<string> GetObjectsToManipulateConfigurations()
            => ["queue-a", "queue-b"];

        protected override void ManipulateObject(IChannel channel, string objectToManipulateConfig)
        {
            ManipulatedObjects.Add(objectToManipulateConfig);
        }
    }

    [Test]
    public void BaseElasticProbe_Run_ShouldInitializeElasticClientAndInvokeProbe()
    {
        var probe = new TestableElasticProbe
        {
            Configuration = new EmptyElasticIndicesConfig
            {
                Url = "http://localhost:9200",
                Username = "user",
                Password = "pass",
                RequestTimeoutMs = 1234
            },
            Context = Globals.Context
        };

        probe.Run([], []);

        Assert.That(probe.RunElasticProbeCalled, Is.True);
        Assert.That(probe.ObservedClient, Is.TypeOf<ElasticClient>());
    }

    [Test]
    public void BaseRedisProbe_RunAndDispose_ShouldUseInjectedConnectionMultiplexer()
    {
        var databaseMock = new Mock<IDatabase>();
        var connectionMock = new Mock<IConnectionMultiplexer>();
        connectionMock.Setup(connection => connection.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(databaseMock.Object);

        var probe = new TestableRedisProbe(connectionMock.Object)
        {
            Configuration = new RedisDataBaseBatchProbeConfig(),
            Context = Globals.Context
        };
        SetRedisHostNames(probe.Configuration, "localhost");

        probe.Run([], []);
        probe.Dispose();

        Assert.That(probe.RunRedisProbeCalled, Is.True);
        Assert.That(probe.ObservedDatabase, Is.SameAs(databaseMock.Object));
        connectionMock.Verify(connection => connection.Dispose(), Times.Once);
    }

    [Test]
    public void BaseOsProbe_RunAndDispose_ShouldUseCreatedConnection()
    {
        using var server = new TestHttpServer();
        var kubernetesClient = server.CreateKubernetesClient();

        var probe = new TestableOsProbe(kubernetesClient)
        {
            Configuration = new OsProbeConfig
            {
                Openshift = CreateOpenshiftConfig()
            },
            Context = Globals.Context
        };

        probe.Run([], []);
        Assert.DoesNotThrow(() => probe.Dispose());

        Assert.That(probe.RunOsProbeCalled, Is.True);
    }

    [Test]
    public void BaseS3Probe_RunAndDispose_ShouldInitializeClients()
    {
        var probe = new TestableS3Probe
        {
            Configuration = new EmptyS3BucketConfig
            {
                AccessKey = "access-key",
                SecretKey = "secret-key",
                ServiceURL = "http://127.0.0.1:9000",
                ForcePathStyle = true,
                StorageBucket = "bucket"
            },
            Context = Globals.Context
        };

        probe.Run([], []);

        Assert.That(probe.RunS3ProbeCalled, Is.True);
        Assert.That(probe.ObservedS3Client, Is.Not.Null);
        Assert.That(probe.ObservedTransferClient, Is.Not.Null);
        Assert.DoesNotThrow(() => probe.Dispose());
    }

    [Test]
    public void BaseRabbitMqObjectsManipulation_Run_ShouldCreateChannelAndManipulateEachConfiguredObject()
    {
        var channelMock = new Mock<IChannel>();
        var connectionMock = new Mock<IConnection>();
        connectionMock.Setup(connection => connection.CreateChannelAsync(It.IsAny<CreateChannelOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(channelMock.Object);

        var connectionFactoryMock = new Mock<IConnectionFactory>();
        connectionFactoryMock.Setup(factory => factory.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(connectionMock.Object);

        var probe = new TestableRabbitMqRunProbe(connectionFactoryMock.Object)
        {
            Configuration = new DeleteRabbitMqQueuesConfig
            {
                Host = "rabbit-host",
                Port = 5678,
                Username = "user",
                Password = "pass",
                VirtualHost = "vhost",
                QueueNames = ["queue-a", "queue-b"]
            },
            Context = Globals.Context
        };

        probe.Run([], []);

        Assert.That(probe.ManipulatedObjects, Is.EqualTo(new[] { "queue-a", "queue-b" }));
        connectionFactoryMock.Verify(factory => factory.CreateConnectionAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        connectionMock.Verify(connection => connection.CreateChannelAsync(It.IsAny<CreateChannelOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Openshift CreateOpenshiftConfig()
    {
        return new Openshift
        {
            Cluster = "cluster",
            Namespace = "namespace",
            Username = "username",
            Password = "password"
        };
    }

    private static void SetRedisHostNames(BaseRedisConfig config, params string[] hostNames)
    {
        var hostNamesProperty =
            typeof(BaseRedisConfig).GetProperty("HostNames", BindingFlags.Public | BindingFlags.Instance)!;
        var hostNamesType = hostNamesProperty.PropertyType;
        object value = hostNamesType == typeof(string[]) ? hostNames : hostNames.ToList();
        hostNamesProperty.SetValue(config, value);
    }
}
