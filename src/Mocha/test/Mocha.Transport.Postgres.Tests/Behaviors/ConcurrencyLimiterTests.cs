using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public class ConcurrencyLimiterTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);
    private readonly PostgresFixture _fixture;

    public ConcurrencyLimiterTests(PostgresFixture fixture)
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

        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddConcurrencyLimiter(o => o.MaxConcurrency = 1)
            .AddEventHandler<SlowOrderHandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);
                t.Endpoint("slow-ep").Handler<SlowOrderHandler>().MaxConcurrency(5).MaxBatchSize(20);
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
