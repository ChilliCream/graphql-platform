using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Middlewares;
using Mocha.Transport.RabbitMQ.Features;
using Mocha.Transport.RabbitMQ.Middlewares;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ.Tests.Middlewares;

public sealed class RabbitMQAcknowledgementMiddlewareTests
{
    private static readonly ShutdownEventArgs s_connectionForced =
        new(ShutdownInitiator.Peer, 320, "CONNECTION_FORCED - test");

    [Fact]
    public async Task InvokeAsync_Should_Complete_When_ChannelClosesDuringAck()
    {
        // arrange
        // the channel reports open, and closes before the ack reaches it
        var channel = new Mock<IChannel>();
        channel.SetupGet(c => c.IsOpen).Returns(true);
        channel
            .Setup(c => c.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException(new AlreadyClosedException(s_connectionForced)));
        var logs = new LogRecorder();
        var context = CreateContext(channel.Object, logs, redelivered: false);

        // act
        var exception = await Record.ExceptionAsync(
            async () => await new RabbitMQAcknowledgementMiddleware().InvokeAsync(context, _ => default));

        // assert
        Assert.Null(exception);
        Assert.True(logs.Contains("SettlementSkippedChannelClosed"));
    }

    [Fact]
    public async Task InvokeAsync_Should_RunHandler_When_MessageIsRedelivered()
    {
        // arrange
        var channel = CreateOpenChannel();
        var context = CreateContext(channel.Object, new LogRecorder(), redelivered: true);
        var handled = false;

        // act
        await new RabbitMQAcknowledgementMiddleware().InvokeAsync(
            context,
            _ =>
            {
                handled = true;
                return default;
            });

        // assert
        Assert.True(handled);
        channel.Verify(c => c.BasicAckAsync(7, false, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAsync_Should_RunHandler_When_MessageIsNotRedelivered()
    {
        // arrange
        var channel = CreateOpenChannel();
        var context = CreateContext(channel.Object, new LogRecorder(), redelivered: false);
        var handled = false;

        // act
        await new RabbitMQAcknowledgementMiddleware().InvokeAsync(
            context,
            _ =>
            {
                handled = true;
                return default;
            });

        // assert
        Assert.True(handled);
        channel.Verify(c => c.BasicAckAsync(7, false, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAsync_Should_RethrowHandlerException_When_NackFailsOnClosedChannel()
    {
        // arrange
        var channel = new Mock<IChannel>();
        channel.SetupGet(c => c.IsOpen).Returns(true);
        channel
            .Setup(c => c.BasicNackAsync(
                It.IsAny<ulong>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException(new AlreadyClosedException(s_connectionForced)));
        var context = CreateContext(channel.Object, new LogRecorder(), redelivered: false);

        // act
        var exception = await Record.ExceptionAsync(
            async () => await new RabbitMQAcknowledgementMiddleware().InvokeAsync(
                context,
                _ => throw new InvalidOperationException("handler failed")));

        // assert
        Assert.Equal("handler failed", Assert.IsType<InvalidOperationException>(exception).Message);
    }

    private static Mock<IChannel> CreateOpenChannel()
    {
        var channel = new Mock<IChannel>();
        channel.SetupGet(c => c.IsOpen).Returns(true);
        channel
            .Setup(c => c.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        return channel;
    }

    private static ReceiveContext CreateContext(
        IChannel channel,
        LogRecorder logs,
        bool redelivered)
    {
        var services = new ServiceCollection()
            .AddLogging(builder => builder.AddProvider(logs))
            .BuildServiceProvider();
        var context = new ReceiveContext { Services = services };

        var feature = context.Features.GetOrSet<RabbitMQReceiveFeature>();
        feature.Channel = channel;
        feature.EventArgs = new BasicDeliverEventArgs(
            consumerTag: "consumer-1",
            deliveryTag: 7,
            redelivered: redelivered,
            exchange: "",
            routingKey: "orders",
            properties: new BasicProperties { MessageId = "message-1" },
            body: ReadOnlyMemory<byte>.Empty);

        return context;
    }
}
