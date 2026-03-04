using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Common.Probes.RabbitMqProbes;
using RabbitMQ.Client;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class RabbitMqBaseProbeTests
{
    private sealed class TestableBaseRabbitProbe
        : BaseRabbitMqObjectsManipulation<DeleteRabbitMqQueuesConfig, string>
    {
        public IConnectionFactory InvokeGetRabbitConnection() => GetRabbitConnection();

        public string ConnectionString => RabbitmqConnectionString;

        protected override IEnumerable<string> GetObjectsToManipulateConfigurations() => [];

        protected override void ManipulateObject(IChannel channel, string objectToManipulateConfig)
        {
        }
    }

    [Test]
    public void TestGetRabbitConnection_ShouldBuildConnectionFactoryFromConfiguration()
    {
        // Arrange
        var probe = new TestableBaseRabbitProbe
        {
            Configuration = new DeleteRabbitMqQueuesConfig
            {
                Host = "rabbit-host",
                Port = 5678,
                Username = "user",
                Password = "pass",
                VirtualHost = "vhost",
                QueueNames = ["q1"]
            },
            Context = Globals.Context
        };

        // Act
        var factory = probe.InvokeGetRabbitConnection();

        // Assert
        var connectionFactory = factory as ConnectionFactory;
        Assert.That(connectionFactory, Is.Not.Null);
        Assert.That(probe.ConnectionString, Is.EqualTo("amqp://user:pass@rabbit-host:5678"));
        Assert.That(connectionFactory!.Uri.Host, Is.EqualTo("rabbit-host"));
        Assert.That(connectionFactory.Uri.Port, Is.EqualTo(5678));
        Assert.That(connectionFactory.VirtualHost, Is.EqualTo("vhost"));
    }
}
