using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Behaviors;

[Collection("AzureServiceBus")]
public class ConcurrencyTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);
    private readonly AzureServiceBusFixture _fixture;

    public ConcurrencyTests(AzureServiceBusFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handler_Should_LimitConcurrency_When_MaxConcurrencySetToOne()
    {
        // arrange - the framework's MaxConcurrency=1 guarantees that no two handler invocations
        // ever overlap. We do not need any artificial in-handler work to assert peak concurrency
        // because tracker.PeakConcurrency reads the high-water mark directly.
        var tracker = new ConcurrencyTracker();
        var recorder = new MessageRecorder();
        const int messageCount = 20;

        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<TrackingOrderHandler>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.Endpoint("slow-ep").Handler<TrackingOrderHandler>().MaxConcurrency(1).PrefetchCount(20);
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        for (var i = 0; i < messageCount; i++)
        {
            await messageBus.PublishAsync(new OrderCreated { OrderId = $"ORD-{i}" }, CancellationToken.None);
        }

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout, expectedCount: messageCount),
            $"Handler did not process all {messageCount} messages within timeout");

        Assert.Equal(1, tracker.PeakConcurrency);
    }

    [Fact]
    public async Task Handler_Should_AllowParallelism_When_MaxConcurrencyGreaterThanOne()
    {
        // arrange - ParallelGate forces each in-flight handler to wait until two peers have
        // entered before any of them releases. This deterministically proves that the framework
        // allows concurrent execution beyond a single handler, without depending on sleeps,
        // timing windows, or the broker dispatching all MaxConcurrency slots simultaneously.
        const int maxConcurrency = 5;
        const int messageCount = 20;
        const int expectedOverlap = 2;

        var tracker = new ConcurrencyTracker();
        var recorder = new MessageRecorder();
        var gate = new ParallelGate(expectedConcurrency: expectedOverlap);

        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddSingleton(gate)
            .AddMessageBus()
            .AddEventHandler<GatedParallelOrderHandler>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.Endpoint("slow-ep").Handler<GatedParallelOrderHandler>()
                    .MaxConcurrency(maxConcurrency).PrefetchCount(20);
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        for (var i = 0; i < messageCount; i++)
        {
            await messageBus.PublishAsync(new OrderCreated { OrderId = $"ORD-{i}" }, CancellationToken.None);
        }

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout, expectedCount: messageCount),
            $"Handler did not process all {messageCount} messages within timeout");

        Assert.True(tracker.PeakConcurrency > 1, $"Expected parallelism > 1, but peak was {tracker.PeakConcurrency}");
        Assert.True(
            tracker.PeakConcurrency <= maxConcurrency,
            $"Expected peak concurrency <= {maxConcurrency}, but was {tracker.PeakConcurrency}");
    }

    /// <summary>
    /// Handler used for the MaxConcurrency=1 test. Records entry/exit through the tracker so
    /// the test can read PeakConcurrency without any in-handler work.
    /// </summary>
    public sealed class TrackingOrderHandler(ConcurrencyTracker tracker, MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            tracker.Enter();
            try
            {
                return default;
            }
            finally
            {
                tracker.Exit();
                recorder.Record(message);
            }
        }
    }

    /// <summary>
    /// Handler used for the MaxConcurrency&gt;1 test. Each invocation enters the gate and waits
    /// until N peers are present before being released, guaranteeing observed overlap without
    /// any reliance on timing.
    /// </summary>
    public sealed class GatedParallelOrderHandler(ConcurrencyTracker tracker, MessageRecorder recorder, ParallelGate gate)
        : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            tracker.Enter();
            try
            {
                await gate.WaitForPeersAsync(cancellationToken);
            }
            finally
            {
                tracker.Exit();
                recorder.Record(message);
            }
        }
    }

    /// <summary>
    /// Synchronization primitive that blocks each entering handler until
    /// <paramref name="expectedConcurrency"/> peers are simultaneously waiting, then releases
    /// them all at once. Subsequent arrivals flow straight through so the recorder can drain
    /// the remaining messages. Includes a fallback deadline so a stalled broker cannot cause
    /// the gate to deadlock the test indefinitely.
    /// </summary>
    public sealed class ParallelGate(int expectedConcurrency)
    {
        private static readonly TimeSpan s_fallback = TimeSpan.FromSeconds(15);

        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrived;

        public async Task WaitForPeersAsync(CancellationToken cancellationToken)
        {
            if (_release.Task.IsCompleted)
            {
                return;
            }

            if (Interlocked.Increment(ref _arrived) >= expectedConcurrency)
            {
                _release.TrySetResult();
                return;
            }

            try
            {
                await _release.Task.WaitAsync(s_fallback, cancellationToken);
            }
            catch (TimeoutException)
            {
                // No peer arrived in time; release ourselves so the recorder can drain.
                _release.TrySetResult();
            }
        }
    }
}
