using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Behaviors;

[Collection("AzureServiceBus")]
public class ConcurrencyLimiterTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);
    private readonly AzureServiceBusFixture _fixture;

    public ConcurrencyLimiterTests(AzureServiceBusFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handler_Should_LimitConcurrency_When_ConcurrencyLimiterConfigured()
    {
        // arrange - the global concurrency limiter caps in-flight handler invocations to 1
        // even when the endpoint advertises MaxConcurrency=5 and prefetches 20 messages.
        // We do not need any artificial in-handler work because tracker.PeakConcurrency reads
        // the high-water mark directly; deterministic synchronization comes from the recorder
        // semaphore.
        var tracker = new ConcurrencyTracker();
        var recorder = new MessageRecorder();
        const int messageCount = 20;

        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddConcurrencyLimiter(o => o.MaxConcurrency = 1)
            .AddEventHandler<TrackingOrderHandler>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.Endpoint("slow-ep").Handler<TrackingOrderHandler>().MaxConcurrency(5).PrefetchCount(20);
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish many messages in parallel
        for (var i = 0; i < messageCount; i++)
        {
            await messageBus.PublishAsync(new OrderCreated { OrderId = $"ORD-{i}" }, CancellationToken.None);
        }

        // assert - wait for all messages and check peak concurrency
        Assert.True(
            await recorder.WaitAsync(s_timeout, expectedCount: messageCount),
            $"Handler did not process all {messageCount} messages within timeout");

        Assert.Equal(1, tracker.PeakConcurrency);
    }

    /// <summary>
    /// Handler that records entry/exit through the tracker. The concurrency limiter wraps the
    /// handler invocation, so PeakConcurrency reflects how many concurrent invocations the
    /// limiter actually permitted past its semaphore.
    /// </summary>
    public sealed class TrackingOrderHandler(ConcurrencyTracker tracker, MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            tracker.Enter();
            tracker.Exit();
            recorder.Record(message);
            return default;
        }
    }
}
