using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Behaviors;

public class ConcurrencyTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Handler_Should_LimitConcurrency_When_MaxConcurrencySetToOne()
    {
        // arrange
        var tracker = new ConcurrencyTracker();
        var recorder = new MessageRecorder();
        const int messageCount = 20;

        await using var provider = await InMemoryBusFixture.CreateBusWithTransportAsync(
            b =>
            {
                b.Services.AddSingleton(tracker);
                b.Services.AddSingleton(recorder);
                b.AddEventHandler<SlowOrderHandler>();
            },
            t => t.Endpoint("slow-ep").Handler<SlowOrderHandler>().MaxConcurrency(1));

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        for (var i = 0; i < messageCount; i++)
        {
            await bus.PublishAsync(new OrderCreated { OrderId = $"ORD-{i}" }, CancellationToken.None);
        }

        // assert
        Assert.True(
            await recorder.WaitAsync(Timeout, expectedCount: messageCount),
            $"Handler did not process all {messageCount} messages within timeout");

        Assert.Equal(1, tracker.PeakConcurrency);
    }

    [Fact]
    public async Task Handler_Should_AllowParallelism_When_MaxConcurrencyGreaterThanOne()
    {
        // arrange
        var tracker = new ConcurrencyTracker();
        var recorder = new MessageRecorder();
        const int messageCount = 20;

        await using var provider = await InMemoryBusFixture.CreateBusWithTransportAsync(
            b =>
            {
                b.Services.AddSingleton(tracker);
                b.Services.AddSingleton(recorder);
                b.AddEventHandler<SlowOrderHandler>();
            },
            t => t.Endpoint("slow-ep").Handler<SlowOrderHandler>().MaxConcurrency(5));

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        for (var i = 0; i < messageCount; i++)
        {
            await bus.PublishAsync(new OrderCreated { OrderId = $"ORD-{i}" }, CancellationToken.None);
        }

        // assert
        Assert.True(
            await recorder.WaitAsync(Timeout, expectedCount: messageCount),
            $"Handler did not process all {messageCount} messages within timeout");

        Assert.True(tracker.PeakConcurrency > 1, $"Expected parallelism > 1, but peak was {tracker.PeakConcurrency}");
        Assert.True(tracker.PeakConcurrency <= 5, $"Expected peak concurrency <= 5, but was {tracker.PeakConcurrency}");
    }

    public sealed class SlowOrderHandler(ConcurrencyTracker tracker, MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            tracker.Enter();
            try
            {
                await Task.Delay(200, cancellationToken);
            }
            finally
            {
                tracker.Exit();
                recorder.Record(message);
            }
        }
    }
}
