using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.NATS.Tests.Helpers;
using NATS.Client.Core;

namespace Mocha.Transport.NATS.Tests.Behaviors;

[Collection("NATS")]
public class ConcurrencyLimiterTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);
    private readonly NatsFixture _fixture;

    public ConcurrencyLimiterTests(NatsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handler_Should_LimitConcurrency_When_ConcurrencyLimiterConfigured()
    {
        // arrange
        var tracker = new ConcurrencyTracker();
        var recorder = new MessageRecorder();
        const int messageCount = 20;

        await using var bus = await new ServiceCollection()
            .AddSingleton(new NatsConnection(_fixture.CreateOptions()))
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddConcurrencyLimiter(o => o.MaxConcurrency = 1)
            .AddEventHandler<SlowOrderHandler>()
            .AddNats(t => t.Endpoint("slow-ep").Handler<SlowOrderHandler>().MaxConcurrency(5).MaxPrefetch(20))
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

    public sealed class SlowOrderHandler(ConcurrencyTracker tracker, MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            tracker.Enter();
            try
            {
                await Task.Delay(5, cancellationToken);
            }
            finally
            {
                tracker.Exit();
                recorder.Record(message);
            }
        }
    }
}
