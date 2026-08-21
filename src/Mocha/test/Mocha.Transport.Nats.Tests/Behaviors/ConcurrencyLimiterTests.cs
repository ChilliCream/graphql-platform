using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Nats.Tests.Fixtures;
using Mocha.Transport.Nats.Tests.Helpers;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public class ConcurrencyLimiterTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task Handler_Should_LimitConcurrency_When_ConcurrencyLimiterConfigured()
    {
        // arrange
        // The bus-wide limiter has to win over the endpoint's own MaxConcurrency, which is higher.
        var tracker = new ConcurrencyTracker();
        var recorder = new MessageRecorder();
        const int messageCount = 20;

        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddConcurrencyLimiter(o => o.MaxConcurrency = 1)
            .AddEventHandler<SlowOrderHandler>()
            .AddNats(nats =>
            {
                nats.StreamName(scope.StreamName);
                nats.Endpoint("slow-ep").Handler<SlowOrderHandler>().MaxConcurrency(5);
            })
            .BuildTestBusAsync();

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

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
    public async Task Handler_Should_RunConcurrently_When_EndpointAllowsIt()
    {
        // arrange
        // The counterpart to the test above: without a limiter, MaxConcurrency is what bounds handling,
        // so a peak of one would mean the receive loop is serial regardless of configuration.
        var tracker = new ConcurrencyTracker();
        var recorder = new MessageRecorder();
        const int messageCount = 20;

        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<SlowOrderHandler>()
            .AddNats(nats =>
            {
                nats.StreamName(scope.StreamName);
                nats.Endpoint("fast-ep").Handler<SlowOrderHandler>().MaxConcurrency(5);
            })
            .BuildTestBusAsync();

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
        for (var i = 0; i < messageCount; i++)
        {
            await messageBus.PublishAsync(new OrderCreated { OrderId = $"ORD-{i}" }, CancellationToken.None);
        }

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout, expectedCount: messageCount),
            $"Handler did not process all {messageCount} messages within timeout");

        Assert.InRange(tracker.PeakConcurrency, 2, 5);
    }

    public sealed class SlowOrderHandler(ConcurrencyTracker tracker, MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            tracker.Enter();
            try
            {
                await Task.Delay(25, cancellationToken);
            }
            finally
            {
                tracker.Exit();
                recorder.Record(message);
            }
        }
    }
}
