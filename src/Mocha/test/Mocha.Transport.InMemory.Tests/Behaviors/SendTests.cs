using Microsoft.Extensions.DependencyInjection;
using Mocha;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Behaviors;

public class SendTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task SendAsync_Should_DeliverToHandler_When_RequestHandlerRegistered()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.SendAsync(new ProcessPayment { OrderId = "ORD-1", Amount = 99.99m }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout), "Handler did not receive the request");

        var message = Assert.Single(recorder.Messages);
        var payment = Assert.IsType<ProcessPayment>(message);
        Assert.Equal("ORD-1", payment.OrderId);
        Assert.Equal(99.99m, payment.Amount);
    }

    [Fact]
    public async Task SendAsync_Should_DeliverAsynchronously_When_FireAndForget()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - SendAsync returns immediately without waiting
        await bus.SendAsync(new ProcessPayment { OrderId = "ORD-1", Amount = 99.99m }, CancellationToken.None);

        // assert - handler receives it asynchronously
        Assert.True(await recorder.WaitAsync(Timeout), "Handler did not receive the request");

        var msg = Assert.Single(recorder.Messages);
        var payment = Assert.IsType<ProcessPayment>(msg);
        Assert.Equal("ORD-1", payment.OrderId);
    }

    [Fact]
    public async Task SendAsync_Should_DeliverToCorrectHandler_When_MultipleQueuesExist()
    {
        // arrange - register two different request handlers (two separate queues)
        var paymentRecorder = new MessageRecorder();
        var refundRecorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddKeyedSingleton("payment", paymentRecorder)
            .AddKeyedSingleton("refund", refundRecorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentKeyedHandler>()
            .AddRequestHandler<ProcessRefundKeyedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - send only to payment handler
        await bus.SendAsync(new ProcessPayment { OrderId = "ORD-1", Amount = 50.00m }, CancellationToken.None);

        // assert - payment handler received the message
        Assert.True(await paymentRecorder.WaitAsync(Timeout), "Payment handler did not receive the send message");

        var msg = Assert.Single(paymentRecorder.Messages);
        Assert.IsType<ProcessPayment>(msg);

        // refund handler should NOT have received anything
        // Give it a brief window to ensure nothing arrives
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
        await using var provider = await new ServiceCollection()
            .AddKeyedSingleton("payment", paymentRecorder)
            .AddKeyedSingleton("refund", refundRecorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentKeyedHandler>()
            .AddRequestHandler<ProcessRefundKeyedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - send to both queues
        await bus.SendAsync(new ProcessPayment { OrderId = "ORD-1", Amount = 50.00m }, CancellationToken.None);
        await bus.SendAsync(new ProcessRefund { OrderId = "ORD-2", Amount = 25.00m }, CancellationToken.None);

        // assert - each handler received exactly its own message
        Assert.True(await paymentRecorder.WaitAsync(Timeout), "Payment handler did not receive the message");
        Assert.True(await refundRecorder.WaitAsync(Timeout), "Refund handler did not receive the message");

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
