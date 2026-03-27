using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Behaviors;

public class SchedulingTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task PublishAsync_Should_DeferDelivery_When_ScheduledTimeIsInFuture()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var scheduledTime = TimeProvider.System.GetUtcNow().Add(TimeSpan.FromSeconds(1));

        // act - scheduler enqueues and returns immediately (non-blocking)
        var sw = Stopwatch.StartNew();
        await bus.PublishAsync(
            new OrderCreated { OrderId = "ORD-SCHED-1" },
            new PublishOptions { ScheduledTime = scheduledTime },
            CancellationToken.None);
        sw.Stop();

        // assert - call returned immediately (scheduler dispatches in background)
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(1),
            $"Expected non-blocking return, but took {sw.Elapsed.TotalSeconds}s");

        Assert.True(
            await recorder.WaitAsync(s_timeout),
            "Message should be delivered after scheduled time");

        var message = Assert.Single(recorder.Messages);
        var order = Assert.IsType<OrderCreated>(message);
        Assert.Equal("ORD-SCHED-1", order.OrderId);
    }

    [Fact]
    public async Task SendAsync_Should_DeferDelivery_When_ScheduledTimeIsInFuture()
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

        var scheduledTime = TimeProvider.System.GetUtcNow().Add(TimeSpan.FromSeconds(1));

        // act - scheduler enqueues and returns immediately (non-blocking)
        var sw = Stopwatch.StartNew();
        await bus.SendAsync(
            new ProcessPayment { OrderId = "ORD-SCHED-2", Amount = 99.99m },
            new SendOptions { ScheduledTime = scheduledTime },
            CancellationToken.None);
        sw.Stop();

        // assert - call returned immediately (scheduler dispatches in background)
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(1),
            $"Expected non-blocking return, but took {sw.Elapsed.TotalSeconds}s");

        Assert.True(
            await recorder.WaitAsync(s_timeout),
            "Message should be delivered after scheduled time");

        var message = Assert.Single(recorder.Messages);
        var payment = Assert.IsType<ProcessPayment>(message);
        Assert.Equal("ORD-SCHED-2", payment.OrderId);
        Assert.Equal(99.99m, payment.Amount);
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverImmediately_When_ScheduledTimeIsInPast()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var pastTime = TimeProvider.System.GetUtcNow().AddMinutes(-1);

        // act
        await bus.PublishAsync(
            new OrderCreated { OrderId = "ORD-PAST-1" },
            new PublishOptions { ScheduledTime = pastTime },
            CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout),
            "Message should be delivered immediately when ScheduledTime is in the past");

        var message = Assert.Single(recorder.Messages);
        var order = Assert.IsType<OrderCreated>(message);
        Assert.Equal("ORD-PAST-1", order.OrderId);
    }

    [Fact]
    public async Task SendAsync_Should_DeliverImmediately_When_ScheduledTimeIsInPast()
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

        var pastTime = TimeProvider.System.GetUtcNow().AddMinutes(-1);

        // act
        await bus.SendAsync(
            new ProcessPayment { OrderId = "ORD-PAST-2", Amount = 25.00m },
            new SendOptions { ScheduledTime = pastTime },
            CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout),
            "Message should be delivered immediately when ScheduledTime is in the past");

        var message = Assert.Single(recorder.Messages);
        var payment = Assert.IsType<ProcessPayment>(message);
        Assert.Equal("ORD-PAST-2", payment.OrderId);
        Assert.Equal(25.00m, payment.Amount);
    }

    public sealed class ProcessPaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }
}
