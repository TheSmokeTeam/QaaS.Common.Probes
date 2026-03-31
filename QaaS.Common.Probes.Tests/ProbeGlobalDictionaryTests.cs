using System.Diagnostics;
using System.Collections.Immutable;
using k8s;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.Elastic;
using QaaS.Common.Probes.ConfigurationObjects.MongoDb;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Common.Probes.ConfigurationObjects.Redis;
using QaaS.Common.Probes.ConfigurationObjects.S3;
using QaaS.Common.Probes.ConfigurationObjects.Sql;
using QaaS.Common.Probes.ElasticProbes;
using QaaS.Common.Probes.MongoDbProbes;
using QaaS.Common.Probes.OsProbes;
using QaaS.Common.Probes.RabbitMqProbes;
using QaaS.Common.Probes.RedisProbes;
using QaaS.Common.Probes.S3Probes;
using QaaS.Common.Probes.SqlProbes;
using QaaS.Framework.Configurations;
using QaaS.Framework.SDK.ContextObjects;
using QaaS.Framework.SDK.Session.SessionDataObjects;
using QaaS.Framework.SDK.Session.SessionDataObjects.RunningSessionsObjects;
using RabbitMQ.Client;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class ProbeGlobalDictionaryTests
{
    private sealed class TestableDeleteRabbitMqExchanges(IConnectionFactory connectionFactory)
        : DeleteRabbitMqExchanges
    {
        protected override IConnectionFactory GetRabbitConnection() => connectionFactory;
    }

    private sealed class TestableOsScaleDeploymentPods : OsScaleDeploymentPods
    {
        public void SetClient(Kubernetes kubernetesClient) => Kubernetes = kubernetesClient;

        public void InvokeRunOsProbe() => RunOsProbe();
    }

    [Test]
    public void RabbitMqRecovery_ShouldPopulateMissingCreateConfigurationFromGlobalDict()
    {
        var context = CreateContext("exec-1", "case-1");
        var writer = new TestableDeleteRabbitMqExchanges(CreateRabbitConnectionFactoryMock().Object)
        {
            Context = context
        };

        using (EnterProbeExecutionScope("session-a", "delete-exchanges"))
        {
            Assert.That(writer.LoadAndValidateConfiguration(BuildConfiguration(new DeleteRabbitMqExchangesConfig
            {
                UseGlobalDict = true,
                Host = "rabbit-host",
                Username = "rabbit-user",
                Password = "rabbit-password",
                Port = 5678,
                VirtualHost = "recovery-vhost",
                ExchangeNames = ["deleted-exchange"]
            })), Is.Empty);
            writer.Run([], []);
        }

        var reader = new CreateRabbitMqExchanges { Context = context };
        List<System.ComponentModel.DataAnnotations.ValidationResult>? validationResults;
        using (EnterProbeExecutionScope("session-a", "create-exchanges"))
        {
            validationResults = reader.LoadAndValidateConfiguration(BuildConfiguration(new
            {
                UseGlobalDict = true
            }));
        }

        Assert.That(validationResults, Is.Empty);
        Assert.That(reader.Configuration.Host, Is.EqualTo("rabbit-host"));
        Assert.That(reader.Configuration.VirtualHost, Is.EqualTo("recovery-vhost"));
        Assert.That(reader.Configuration.Exchanges!.Select(exchange => exchange.Name),
            Is.EqualTo(new[] { "deleted-exchange" }));
    }

    [Test]
    public void RabbitMqRecovery_ShouldPreserveExplicitLocalOverrides()
    {
        var context = CreateContext("exec-2", "case-2");
        var writer = new TestableDeleteRabbitMqExchanges(CreateRabbitConnectionFactoryMock().Object)
        {
            Context = context
        };

        using (EnterProbeExecutionScope("session-a", "delete-exchanges"))
        {
            Assert.That(writer.LoadAndValidateConfiguration(BuildConfiguration(new DeleteRabbitMqExchangesConfig
            {
                UseGlobalDict = true,
                Host = "global-rabbit",
                ExchangeNames = ["global-exchange"]
            })), Is.Empty);
            writer.Run([], []);
        }

        var reader = new CreateRabbitMqExchanges { Context = context };
        using (EnterProbeExecutionScope("session-a", "create-exchanges"))
        {
            reader.LoadAndValidateConfiguration(BuildConfiguration(new CreateRabbitMqExchangesConfig
            {
                UseGlobalDict = true,
                Host = "local-rabbit",
                Exchanges =
                [
                    new RabbitMqExchangeConfig
                    {
                        Name = "local-exchange"
                    }
                ]
            }));
        }

        Assert.That(reader.Configuration.Host, Is.EqualTo("local-rabbit"));
        Assert.That(reader.Configuration.Exchanges!.Select(exchange => exchange.Name),
            Is.EqualTo(new[] { "local-exchange" }));
    }

    [Test]
    public void RabbitMqRecovery_ShouldKeepCurrentBehaviorWhenUseGlobalDictIsFalse()
    {
        var context = CreateContext("exec-3", "case-3");
        var writer = new TestableDeleteRabbitMqExchanges(CreateRabbitConnectionFactoryMock().Object)
        {
            Context = context
        };

        using (EnterProbeExecutionScope("session-a", "delete-exchanges"))
        {
            Assert.That(writer.LoadAndValidateConfiguration(BuildConfiguration(new DeleteRabbitMqExchangesConfig
            {
                UseGlobalDict = true,
                Host = "global-rabbit",
                ExchangeNames = ["global-exchange"]
            })), Is.Empty);
            writer.Run([], []);
        }

        var reader = new CreateRabbitMqExchanges { Context = context };
        List<System.ComponentModel.DataAnnotations.ValidationResult>? validationResults;
        using (EnterProbeExecutionScope("session-a", "create-exchanges"))
        {
            validationResults = reader.LoadAndValidateConfiguration(BuildConfiguration(new { }));
        }

        Assert.That(validationResults, Is.Not.Empty);
        Assert.That(validationResults!.Select(result => result.ErrorMessage),
            Has.Some.Contains("required").IgnoreCase);
    }

    [Test]
    public void OsScaleRecovery_ShouldRestorePreviousReplicaCountFromGlobalDict()
    {
        using var server = new TestHttpServer();
        server.EnqueueJsonResponse(
            "GET",
            "/apis/apps/v1/namespaces/namespace/deployments/deployment-a",
            """
            {
              "apiVersion": "apps/v1",
              "kind": "Deployment",
              "metadata": { "name": "deployment-a", "generation": 6 },
              "spec": { "replicas": 5 },
              "status": {
                "observedGeneration": 6,
                "replicas": 5,
                "availableReplicas": 5,
                "updatedReplicas": 5,
                "readyReplicas": 5
              }
            }
            """);
        server.EnqueueJsonResponse(
            "PUT",
            "/apis/apps/v1/namespaces/namespace/deployments/deployment-a",
            """
            {
              "apiVersion": "apps/v1",
              "kind": "Deployment",
              "metadata": { "name": "deployment-a", "generation": 7 },
              "spec": { "replicas": 2 },
              "status": {
                "observedGeneration": 7,
                "replicas": 2,
                "availableReplicas": 2,
                "updatedReplicas": 2,
                "readyReplicas": 2
              }
            }
            """);
        server.EnqueueJsonResponse(
            "GET",
            "/apis/apps/v1/namespaces/namespace/deployments/deployment-a",
            """
            {
              "apiVersion": "apps/v1",
              "kind": "Deployment",
              "metadata": { "name": "deployment-a", "generation": 7 },
              "spec": { "replicas": 2 },
              "status": {
                "observedGeneration": 7,
                "replicas": 2,
                "availableReplicas": 2,
                "updatedReplicas": 2,
                "readyReplicas": 2
              }
            }
            """);

        var context = CreateContext("exec-4", "case-4");
        var writer = new TestableOsScaleDeploymentPods
        {
            Context = context,
            Configuration = new OsScalePodsProbeConfig
            {
                UseGlobalDict = true,
                Openshift = CreateOpenshiftConfig(),
                ReplicaSetName = "deployment-a",
                DesiredNumberOfPods = 2,
                IntervalBetweenDesiredStateChecksMs = 0,
                TimeoutWaitForDesiredStateSeconds = 1
            }
        };
        writer.SetClient(server.CreateKubernetesClient());

        using (EnterProbeExecutionScope("session-a", "scale-down"))
        {
            writer.InvokeRunOsProbe();
        }

        var reader = new OsScaleDeploymentPods { Context = context };
        using (EnterProbeExecutionScope("session-a", "scale-restore"))
        {
            reader.LoadAndValidateConfiguration(BuildConfiguration(new
            {
                UseGlobalDict = true,
                ReplicaSetName = "deployment-a",
                Openshift = CreateOpenshiftConfig()
            }));
        }

        Assert.That(reader.Configuration.DesiredNumberOfPods, Is.EqualTo(5));
    }

    [Test]
    public void NonRabbitOsFamilies_ShouldLoadMissingValuesFromGlobalDict()
    {
        var context = CreateContext("exec-5", "case-5");

        LoadDefaults(context, "session-a", "redis-defaults", new FlushDbRedis { Context = context },
            new RedisDataBaseProbeBaseConfig
            {
                UseGlobalDict = true,
                HostNames = ["redis-a", "redis-b"],
                RedisDataBase = 4
            });
        var redisReader = new ExecuteRedisCommand { Context = context };
        using (EnterProbeExecutionScope("session-a", "redis-reader"))
        {
            Assert.That(redisReader.LoadAndValidateConfiguration(BuildConfiguration(new
            {
                UseGlobalDict = true,
                Command = "PING"
            })), Is.Empty);
        }

        LoadDefaults(context, "session-a", "elastic-defaults", new DeleteElasticIndices { Context = context },
            new DeleteElasticIndicesConfig
            {
                UseGlobalDict = true,
                Url = "http://elastic.local",
                Username = "elastic-user",
                Password = "elastic-password",
                IndexPattern = "orders-*"
            });
        var elasticReader = new EmptyElasticIndices { Context = context };
        using (EnterProbeExecutionScope("session-a", "elastic-reader"))
        {
            Assert.That(elasticReader.LoadAndValidateConfiguration(BuildConfiguration(new
            {
                UseGlobalDict = true,
                IndexPattern = "orders-*"
            })), Is.Empty);
        }

        LoadDefaults(context, "session-a", "s3-defaults", new CreateS3Bucket { Context = context },
            new CreateS3BucketConfig
            {
                UseGlobalDict = true,
                StorageBucket = "qaas-bucket",
                ServiceURL = "http://minio.local",
                AccessKey = "access",
                SecretKey = "secret"
            });
        var s3Reader = new EmptyS3Bucket { Context = context };
        using (EnterProbeExecutionScope("session-a", "s3-reader"))
        {
            Assert.That(s3Reader.LoadAndValidateConfiguration(BuildConfiguration(new
            {
                UseGlobalDict = true,
                Prefix = "recover/"
            })), Is.Empty);
        }

        LoadDefaults(context, "session-a", "mongo-defaults", new DropMongoDbCollection { Context = context },
            new DropMongoDbCollectionConfig
            {
                UseGlobalDict = true,
                ConnectionString = "mongodb://mongo.local",
                DatabaseName = "qaas",
                CollectionName = "events"
            });
        var mongoReader = new EmptyMongoDbCollection { Context = context };
        using (EnterProbeExecutionScope("session-a", "mongo-reader"))
        {
            Assert.That(mongoReader.LoadAndValidateConfiguration(BuildConfiguration(new
            {
                UseGlobalDict = true
            })), Is.Empty);
        }

        LoadDefaults(context, "session-a", "sql-defaults", new MsSqlDataBaseTablesTruncate { Context = context },
            new SqlDataBaseTablesTruncateProbeConfig
            {
                UseGlobalDict = true,
                ConnectionString = "Server=db;Database=qaas;User Id=sa;Password=pass;",
                TableNames = ["events"]
            });
        var sqlReader = new PostgreSqlDataBaseTablesTruncate { Context = context };
        using (EnterProbeExecutionScope("session-a", "sql-reader"))
        {
            Assert.That(sqlReader.LoadAndValidateConfiguration(BuildConfiguration(new
            {
                UseGlobalDict = true,
                TableNames = new[] { "events" }
            })), Is.Empty);
        }

        Assert.Multiple(() =>
        {
            Assert.That(redisReader.Configuration.HostNames, Is.EqualTo(new[] { "redis-a", "redis-b" }));
            Assert.That(redisReader.Configuration.RedisDataBase, Is.EqualTo(4));
            Assert.That(elasticReader.Configuration.Url, Is.EqualTo("http://elastic.local"));
            Assert.That(s3Reader.Configuration.StorageBucket, Is.EqualTo("qaas-bucket"));
            Assert.That(mongoReader.Configuration.ConnectionString, Is.EqualTo("mongodb://mongo.local"));
            Assert.That(sqlReader.Configuration.ConnectionString, Does.Contain("Server=db"));
        });
    }

    private static Mock<IConnectionFactory> CreateRabbitConnectionFactoryMock()
    {
        var channelMock = new Mock<IChannel>();
        channelMock.Setup(channel => channel.ExchangeDeleteAsync(
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var connectionMock = new Mock<IConnection>();
        connectionMock.Setup(connection => connection.CreateChannelAsync(It.IsAny<CreateChannelOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(channelMock.Object);

        var connectionFactoryMock = new Mock<IConnectionFactory>();
        connectionFactoryMock.Setup(factory => factory.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(connectionMock.Object);
        return connectionFactoryMock;
    }

    private static InternalContext CreateContext(string executionId, string caseName)
    {
        return new InternalContext
        {
            Logger = Globals.Logger,
            ExecutionId = executionId,
            CaseName = caseName,
            InternalRunningSessions = new RunningSessions(new Dictionary<string, RunningSessionData<object, object>>())
        };
    }

    private static void LoadDefaults<TProbe, TConfiguration>(InternalContext context, string sessionName, string probeName,
        TProbe probe, TConfiguration configuration)
        where TProbe : class, QaaS.Framework.SDK.Hooks.Probe.IProbe
        where TConfiguration : notnull
    {
        using var scope = EnterProbeExecutionScope(sessionName, probeName);
        Assert.That(probe.LoadAndValidateConfiguration(BuildConfiguration(configuration)), Is.Empty);
    }

    private static IDisposable EnterProbeExecutionScope(string sessionName, string probeName)
    {
        var activity = new Activity("QaaS.ProbeExecutionScope");
        activity.AddBaggage("qaas.probe.session-name", sessionName);
        activity.AddBaggage("qaas.probe.probe-name", probeName);
        activity.Start();
        return new ActivityScope(activity);
    }

    private static IConfiguration BuildConfiguration(object configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new ConfigurationBuilder()
            .AddInMemoryCollection(ConfigurationUtils.GetInMemoryCollectionFromObject(configuration))
            .Build();
    }

    private static Openshift CreateOpenshiftConfig()
    {
        return new Openshift
        {
            Cluster = "https://cluster.local",
            Namespace = "namespace",
            Username = "user",
            Password = "password"
        };
    }

    private sealed class ActivityScope(Activity activity) : IDisposable
    {
        public void Dispose()
        {
            activity.Stop();
        }
    }
}
