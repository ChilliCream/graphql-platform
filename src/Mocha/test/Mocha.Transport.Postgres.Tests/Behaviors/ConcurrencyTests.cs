using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public class ConcurrencyTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);
    private readonly PostgresFixture _fixture;

    public ConcurrencyTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handler_Should_LimitConcurrency_When_MaxConcurrencySetToOne()
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
            .AddEventHandler<SlowOrderHandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);
                t.Endpoint("slow-ep").Handler<SlowOrderHandler>().MaxConcurrency(1).MaxBatchSize(20);
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
        // arrange
        var tracker = new ConcurrencyTracker();
        var recorder = new MessageRecorder();
        const int messageCount = 20;

        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<SlowOrderHandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);
                t.Endpoint("slow-ep").Handler<SlowOrderHandler>().MaxConcurrency(5).MaxBatchSize(20);
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
                await Task.Delay(500, cancellationToken);
            }
            finally
            {
                tracker.Exit();
                recorder.Record(message);
            }
        }
    }
}
