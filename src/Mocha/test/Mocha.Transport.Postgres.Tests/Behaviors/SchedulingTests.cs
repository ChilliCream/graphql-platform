using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public class SchedulingTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly PostgresFixture _fixture;

    public SchedulingTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishAsync_Should_DeferDelivery_When_ScheduledTimeIsInFuture()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddPostgres(t => t.ConnectionString(db.ConnectionString))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var scheduledTime = TimeProvider.System.GetUtcNow().AddSeconds(4);

        // act
        await messageBus.PublishAsync(
            new OrderCreated { OrderId = "ORD-SCHED-1" },
            new PublishOptions { ScheduledTime = scheduledTime },
            CancellationToken.None);

        // assert - should NOT be delivered yet
        Assert.False(
            await recorder.WaitAsync(TimeSpan.FromSeconds(2)),
            "Message should not be visible before scheduled time");

        // should be delivered after scheduled time
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
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddPostgres(t => t.ConnectionString(db.ConnectionString))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var scheduledTime = TimeProvider.System.GetUtcNow().AddSeconds(4);

        // act
        await messageBus.SendAsync(
            new ProcessPayment { OrderId = "ORD-SCHED-2", Amount = 99.99m },
            new SendOptions { ScheduledTime = scheduledTime },
            CancellationToken.None);

        // assert - should NOT be delivered yet
        Assert.False(
            await recorder.WaitAsync(TimeSpan.FromSeconds(2)),
            "Message should not be visible before scheduled time");

        // should be delivered after scheduled time
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
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddPostgres(t => t.ConnectionString(db.ConnectionString))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var pastTime = TimeProvider.System.GetUtcNow().AddMinutes(-1);

        // act
        await messageBus.PublishAsync(
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
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddPostgres(t => t.ConnectionString(db.ConnectionString))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var pastTime = TimeProvider.System.GetUtcNow().AddMinutes(-1);

        // act
        await messageBus.SendAsync(
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
