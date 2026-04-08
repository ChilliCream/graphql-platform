using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Behaviors;

public class ConcurrencyLimiterTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Handler_Should_LimitConcurrency_When_ConcurrencyLimiterConfigured()
    {
        // arrange
        var tracker = new ConcurrencyTracker();
        const int messageCount = 20;

        await using var provider = await new ServiceCollection()
            .AddSingleton(tracker)
            .AddMessageBus()
            .AddConcurrencyLimiter(o => o.MaxConcurrency = 1)
            .AddEventHandler<SlowOrderHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish many messages in parallel
        for (var i = 0; i < messageCount; i++)
        {
            await bus.PublishAsync(new OrderCreated { OrderId = $"ORD-{i}" }, CancellationToken.None);
        }

        // assert - wait for all messages and check peak concurrency
        Assert.True(
            await tracker.WaitAsync(s_timeout, messageCount),
            $"Handler did not process all {messageCount} messages");

        Assert.Equal(1, tracker.PeakConcurrency);
    }

    public sealed class SlowOrderHandler(ConcurrencyTracker tracker) : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            tracker.Enter();
            try
            {
                // Small delay to allow overlap detection
                await Task.Delay(5, cancellationToken);
            }
            finally
            {
                tracker.Exit();
            }
        }
    }
}
