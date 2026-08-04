using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
public class SendTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly RabbitMQFixture _fixture;

    public SendTests(RabbitMQFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SendAsync_Should_DeliverToHandler_When_RequestHandlerRegistered()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.SendAsync(new ProcessPayment { OrderId = "ORD-1", Amount = 99.99m }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the request");

        var message = Assert.Single(recorder.Messages);
        var payment = Assert.IsType<ProcessPayment>(message);
        Assert.Equal("ORD-1", payment.OrderId);
        Assert.Equal(99.99m, payment.Amount);
    }

    [Fact]
    public async Task SendAsync_Should_DeliverToCorrectHandler_When_MultipleQueuesExist()
    {
        // arrange
        var paymentRecorder = new MessageRecorder();
        var refundRecorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddKeyedSingleton("payment", paymentRecorder)
            .AddKeyedSingleton("refund", refundRecorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentKeyedHandler>()
            .AddRequestHandler<ProcessRefundKeyedHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.SendAsync(new ProcessPayment { OrderId = "ORD-1", Amount = 50.00m }, CancellationToken.None);

        // assert
        Assert.True(await paymentRecorder.WaitAsync(s_timeout), "Payment handler did not receive the send message");

        var msg = Assert.Single(paymentRecorder.Messages);
        Assert.IsType<ProcessPayment>(msg);

        Assert.False(
            await refundRecorder.WaitAsync(TimeSpan.FromMilliseconds(500)),
            "Refund handler should not have received a message intended for payment queue");
        Assert.Empty(refundRecorder.Messages);
    }

    [Fact]
    public async Task SendAsync_Should_DeliverToEachHandler_When_SendingToDifferentQueues()
    {
        // arrange
        var paymentRecorder = new MessageRecorder();
        var refundRecorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddKeyedSingleton("payment", paymentRecorder)
            .AddKeyedSingleton("refund", refundRecorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentKeyedHandler>()
            .AddRequestHandler<ProcessRefundKeyedHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.SendAsync(new ProcessPayment { OrderId = "ORD-1", Amount = 50.00m }, CancellationToken.None);
        await messageBus.SendAsync(new ProcessRefund { OrderId = "ORD-2", Amount = 25.00m }, CancellationToken.None);

        // assert
        Assert.True(await paymentRecorder.WaitAsync(s_timeout), "Payment handler did not receive the message");
        Assert.True(await refundRecorder.WaitAsync(s_timeout), "Refund handler did not receive the message");

        var payment = Assert.IsType<ProcessPayment>(Assert.Single(paymentRecorder.Messages));
        Assert.Equal("ORD-1", payment.OrderId);

        var refund = Assert.IsType<ProcessRefund>(Assert.Single(refundRecorder.Messages));
        Assert.Equal("ORD-2", refund.OrderId);
    }

    public sealed class ProcessPaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }

    public sealed class ProcessRefund
    {
        public required string OrderId { get; init; }
        public required decimal Amount { get; init; }
    }

    public sealed class ProcessPaymentKeyedHandler([FromKeyedServices("payment")] MessageRecorder recorder)
        : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }

    public sealed class ProcessRefundKeyedHandler([FromKeyedServices("refund")] MessageRecorder recorder)
        : IEventRequestHandler<ProcessRefund>
    {
        public ValueTask HandleAsync(ProcessRefund request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }
}
