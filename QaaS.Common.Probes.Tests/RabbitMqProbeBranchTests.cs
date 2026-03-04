using Moq;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Common.Probes.RabbitMqProbes;
using RabbitMQ.Client;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class RabbitMqProbeBranchTests
{
    private sealed class TestableCreateRabbitMqBindings : CreateRabbitMqBindings
    {
        public void InvokeManipulateObject(IChannel channel, RabbitMqBindingConfig config) => ManipulateObject(channel, config);
    }

    private sealed class TestableDeleteRabbitMqBindings : DeleteRabbitMqBindings
    {
        public void InvokeManipulateObject(IChannel channel, RabbitMqBindingConfig config) => ManipulateObject(channel, config);
    }

    private sealed class TestableCreateRabbitMqExchanges : CreateRabbitMqExchanges
    {
        public void InvokeManipulateObject(IChannel channel, RabbitMqExchangeConfig config) => ManipulateObject(channel, config);
    }

    private sealed class TestableCreateRabbitMqQueues : CreateRabbitMqQueues
    {
        public void InvokeManipulateObject(IChannel channel, RabbitMqQueueConfig config) => ManipulateObject(channel, config);
    }

    private sealed class TestableDeleteRabbitMqQueues : DeleteRabbitMqQueues
    {
        public void InvokeManipulateObject(IChannel channel, string queueName) => ManipulateObject(channel, queueName);
    }

    private sealed class TestableDeleteRabbitMqExchanges : DeleteRabbitMqExchanges
    {
        public void InvokeManipulateObject(IChannel channel, string exchangeName) => ManipulateObject(channel, exchangeName);
    }

    private sealed class TestablePurgeRabbitMqQueues : PurgeRabbitMqQueues
    {
        public void InvokeManipulateObject(IChannel channel, string queueName) => ManipulateObject(channel, queueName);
    }

    [Test]
    public void TestCreateBindings_WhenBindingTypeIsExchangeToQueue_ShouldCallQueueBind()
    {
        // Arrange
        var channelMock = new Mock<IChannel>();
        channelMock.Setup(m => m.QueueBindAsync(
                "destination", "source", "route", It.IsAny<IDictionary<string, object?>>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var probe = new TestableCreateRabbitMqBindings { Context = Globals.Context };
        var arguments = new Dictionary<string, object?> { ["header"] = "value" };

        // Act
        probe.InvokeManipulateObject(channelMock.Object, new RabbitMqBindingConfig
        {
            BindingType = BindingType.ExchangeToQueue,
            DestinationName = "destination",
            SourceName = "source",
            RoutingKey = "route",
            Arguments = arguments
        });

        // Assert
        channelMock.Verify(m => m.QueueBindAsync(
            "destination", "source", "route", arguments, It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void TestCreateBindings_WhenBindingTypeIsExchangeToExchange_ShouldCallExchangeBind()
    {
        // Arrange
        var channelMock = new Mock<IChannel>();
        channelMock.Setup(m => m.ExchangeBindAsync(
                "destination", "source", "route", It.IsAny<IDictionary<string, object?>>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var probe = new TestableCreateRabbitMqBindings { Context = Globals.Context };
        var arguments = new Dictionary<string, object?> { ["header"] = "value" };

        // Act
        probe.InvokeManipulateObject(channelMock.Object, new RabbitMqBindingConfig
        {
            BindingType = BindingType.ExchangeToExchange,
            DestinationName = "destination",
            SourceName = "source",
            RoutingKey = "route",
            Arguments = arguments
        });

        // Assert
        channelMock.Verify(m => m.ExchangeBindAsync(
            "destination", "source", "route", arguments, It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void TestCreateBindings_WhenBindingTypeIsUnsupported_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var probe = new TestableCreateRabbitMqBindings { Context = Globals.Context };

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => probe.InvokeManipulateObject(new Mock<IChannel>().Object,
            new RabbitMqBindingConfig
            {
                BindingType = (BindingType)999,
                DestinationName = "destination",
                SourceName = "source"
            }));
    }

    [Test]
    public void TestDeleteBindings_WhenBindingTypeIsExchangeToQueue_ShouldCallQueueUnbindWithArguments()
    {
        // Arrange
        var channelMock = new Mock<IChannel>();
        channelMock.Setup(m => m.QueueUnbindAsync(
                "destination", "source", "route", It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var probe = new TestableDeleteRabbitMqBindings { Context = Globals.Context };
        var arguments = new Dictionary<string, object?> { ["header"] = "value" };

        // Act
        probe.InvokeManipulateObject(channelMock.Object, new RabbitMqBindingConfig
        {
            BindingType = BindingType.ExchangeToQueue,
            DestinationName = "destination",
            SourceName = "source",
            RoutingKey = "route",
            Arguments = arguments
        });

        // Assert
        channelMock.Verify(m => m.QueueUnbindAsync(
            "destination", "source", "route", arguments, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void TestDeleteBindings_WhenBindingTypeIsExchangeToExchange_ShouldCallExchangeUnbindWithArguments()
    {
        // Arrange
        var channelMock = new Mock<IChannel>();
        channelMock.Setup(m => m.ExchangeUnbindAsync(
                "destination", "source", "route", It.IsAny<IDictionary<string, object?>>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var probe = new TestableDeleteRabbitMqBindings { Context = Globals.Context };
        var arguments = new Dictionary<string, object?> { ["header"] = "value" };

        // Act
        probe.InvokeManipulateObject(channelMock.Object, new RabbitMqBindingConfig
        {
            BindingType = BindingType.ExchangeToExchange,
            DestinationName = "destination",
            SourceName = "source",
            RoutingKey = "route",
            Arguments = arguments
        });

        // Assert
        channelMock.Verify(m => m.ExchangeUnbindAsync(
            "destination", "source", "route", arguments, It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void TestDeleteBindings_WhenBindingTypeIsUnsupported_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var probe = new TestableDeleteRabbitMqBindings { Context = Globals.Context };

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => probe.InvokeManipulateObject(new Mock<IChannel>().Object,
            new RabbitMqBindingConfig
            {
                BindingType = (BindingType)999,
                DestinationName = "destination",
                SourceName = "source"
            }));
    }

    [Test]
    public void TestCreateExchanges_WhenTypeIsConsistentHash_ShouldUseExpectedExchangeType()
    {
        // Arrange
        var channelMock = new Mock<IChannel>();
        channelMock.Setup(m => m.ExchangeDeclareAsync(
                "exchange-a", "x-consistent-hash", true, false, It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var probe = new TestableCreateRabbitMqExchanges { Context = Globals.Context };

        // Act
        probe.InvokeManipulateObject(channelMock.Object, new RabbitMqExchangeConfig
        {
            Name = "exchange-a",
            Type = RabbitMqExchangeType.ConsistentHash,
            Durable = true,
            AutoDelete = false
        });

        // Assert
        channelMock.VerifyAll();
    }

    [Test]
    public void TestCreateExchanges_WhenTypeIsDirect_ShouldLowerCaseExchangeType()
    {
        // Arrange
        var channelMock = new Mock<IChannel>();
        channelMock.Setup(m => m.ExchangeDeclareAsync(
                "exchange-b", "direct", false, true, It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var probe = new TestableCreateRabbitMqExchanges { Context = Globals.Context };

        // Act
        probe.InvokeManipulateObject(channelMock.Object, new RabbitMqExchangeConfig
        {
            Name = "exchange-b",
            Type = RabbitMqExchangeType.Direct,
            Durable = false,
            AutoDelete = true
        });

        // Assert
        channelMock.VerifyAll();
    }

    [Test]
    public void TestCreateQueues_ShouldCallQueueDeclare()
    {
        // Arrange
        var channelMock = new Mock<IChannel>();
        channelMock.Setup(m => m.QueueDeclareAsync(
                "queue-a", true, false, false, It.IsAny<IDictionary<string, object?>>(), It.IsAny<bool>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(default(QueueDeclareOk)!));

        var probe = new TestableCreateRabbitMqQueues { Context = Globals.Context };

        // Act
        probe.InvokeManipulateObject(channelMock.Object, new RabbitMqQueueConfig
        {
            Name = "queue-a",
            Durable = true,
            Exclusive = false,
            AutoDelete = false
        });

        // Assert
        channelMock.VerifyAll();
    }

    [Test]
    public void TestDeleteQueues_ShouldCallQueueDelete()
    {
        // Arrange
        var channelMock = new Mock<IChannel>();
        channelMock.Setup(m => m.QueueDeleteAsync(
                "queue-a", It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((uint)0);

        var probe = new TestableDeleteRabbitMqQueues { Context = Globals.Context };

        // Act
        probe.InvokeManipulateObject(channelMock.Object, "queue-a");

        // Assert
        channelMock.Verify(m => m.QueueDeleteAsync(
            "queue-a", It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void TestDeleteExchanges_ShouldCallExchangeDelete()
    {
        // Arrange
        var channelMock = new Mock<IChannel>();
        channelMock.Setup(m => m.ExchangeDeleteAsync(
                "exchange-a", It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var probe = new TestableDeleteRabbitMqExchanges { Context = Globals.Context };

        // Act
        probe.InvokeManipulateObject(channelMock.Object, "exchange-a");

        // Assert
        channelMock.Verify(m => m.ExchangeDeleteAsync(
            "exchange-a", It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void TestPurgeQueues_ShouldCallQueuePurge()
    {
        // Arrange
        var channelMock = new Mock<IChannel>();
        channelMock.Setup(m => m.QueuePurgeAsync("queue-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync((uint)17);

        var probe = new TestablePurgeRabbitMqQueues { Context = Globals.Context };

        // Act
        probe.InvokeManipulateObject(channelMock.Object, "queue-a");

        // Assert
        channelMock.Verify(m => m.QueuePurgeAsync("queue-a", It.IsAny<CancellationToken>()), Times.Once);
    }
}
