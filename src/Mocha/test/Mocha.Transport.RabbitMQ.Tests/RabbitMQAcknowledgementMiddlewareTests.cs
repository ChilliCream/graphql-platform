using Mocha.Middlewares;
using Mocha.Transport.RabbitMQ.Features;
using Mocha.Transport.RabbitMQ.Middlewares;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ.Tests;

public sealed class RabbitMQAcknowledgementMiddlewareTests
{
    private const ulong DeliveryTag = 7;

    [Fact]
    public async Task InvokeAsync_Should_Ack_When_ReceiveIsCancelledAfterHandlerFinished()
    {
        // arrange
        using var cts = new CancellationTokenSource();
        var channel = CreateOpenChannel();
        var context = CreateContext(channel.Object, cts.Token);
        var middleware = new RabbitMQAcknowledgementMiddleware();

        // act
        await middleware.InvokeAsync(
            context,
            _ =>
            {
                cts.Cancel();
                return ValueTask.CompletedTask;
            });

        // assert
        channel.Verify(
            c => c.BasicAckAsync(DeliveryTag, false, It.Is<CancellationToken>(t => !t.IsCancellationRequested)),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_NackWithRequeue_When_HandlerIsCancelled()
    {
        // arrange
        using var cts = new CancellationTokenSource();
        var channel = CreateOpenChannel();
        var context = CreateContext(channel.Object, cts.Token);
        var middleware = new RabbitMQAcknowledgementMiddleware();

        // act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => middleware.InvokeAsync(
                context,
                _ =>
                {
                    cts.Cancel();
                    throw new OperationCanceledException(cts.Token);
                }).AsTask());

        // assert
        channel.Verify(
            c => c.BasicNackAsync(
                DeliveryTag,
                false,
                true,
                It.Is<CancellationToken>(t => !t.IsCancellationRequested)),
            Times.Once);
    }

    private static Mock<IChannel> CreateOpenChannel()
    {
        var channel = new Mock<IChannel>();
        channel.SetupGet(c => c.IsOpen).Returns(true);
        return channel;
    }

    private static ReceiveContext CreateContext(IChannel channel, CancellationToken cancellationToken)
    {
        var context = new ReceiveContext { CancellationToken = cancellationToken };
        var feature = context.Features.GetOrSet<RabbitMQReceiveFeature>();
        feature.Channel = channel;
        feature.EventArgs = new BasicDeliverEventArgs(
            consumerTag: "tag",
            deliveryTag: DeliveryTag,
            redelivered: false,
            exchange: "exchange",
            routingKey: "key",
            properties: new BasicProperties(),
            body: ReadOnlyMemory<byte>.Empty);

        return context;
    }
}
