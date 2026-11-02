using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;
using Npgsql;

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

    [Fact]
    public async Task ScheduleSendAsync_Should_ReturnCancellableToken_When_PostgresTransportStoreRegistered()
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
        var scheduledTime = TimeProvider.System.GetUtcNow().AddMinutes(10);

        // act
        var result = await messageBus.ScheduleSendAsync(
            new ProcessPayment { OrderId = "ORD-TOKEN", Amount = 42.00m },
            scheduledTime,
            CancellationToken.None);

        // assert
        Assert.True(result.IsCancellable);
        Assert.NotNull(result.Token);
        Assert.StartsWith("postgres-transport:", result.Token);
        Assert.Equal(scheduledTime, result.ScheduledTime);

        // cleanup so the message does not get delivered to a future test run
        await messageBus.CancelScheduledMessageAsync(result.Token, CancellationToken.None);
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_Should_PreventDelivery_When_CalledBeforeScheduledTime()
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
        var scheduledTime = TimeProvider.System.GetUtcNow().AddSeconds(5);

        var result = await messageBus.ScheduleSendAsync(
            new ProcessPayment { OrderId = "ORD-CANCEL", Amount = 10.00m },
            scheduledTime,
            CancellationToken.None);

        // act
        var cancelled = await messageBus.CancelScheduledMessageAsync(result.Token!, CancellationToken.None);

        // assert
        Assert.True(cancelled);
        Assert.False(
            await recorder.WaitAsync(TimeSpan.FromSeconds(7)),
            "Handler should not have received a cancelled scheduled message");
        Assert.Empty(recorder.Messages);
    }

    [Fact]
    public async Task SchedulePublishAsync_Should_DeliverToAllSubscribedQueues_When_TopicHasMultipleSubscribers()
    {
        // arrange
        var recorder1 = new MessageRecorder();
        var recorder2 = new MessageRecorder();
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddKeyedSingleton("r1", recorder1)
            .AddKeyedSingleton("r2", recorder2)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedScheduledHandler1>()
            .AddEventHandler<OrderCreatedScheduledHandler2>()
            .AddPostgres(t => t.ConnectionString(db.ConnectionString))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var scheduledTime = TimeProvider.System.GetUtcNow().AddSeconds(4);

        // act
        var result = await messageBus.SchedulePublishAsync(
            new OrderCreated { OrderId = "ORD-SCHED-MULTI" },
            scheduledTime,
            CancellationToken.None);

        // assert
        Assert.True(result.IsCancellable);
        Assert.NotNull(result.Token);
        Assert.StartsWith("postgres-transport:", result.Token);

        var ids = result.Token!["postgres-transport:".Length..].Split(',');
        Assert.Equal(2, ids.Length);

        Assert.True(
            await recorder1.WaitAsync(s_timeout),
            "First subscribed queue did not receive the scheduled event");
        Assert.True(
            await recorder2.WaitAsync(s_timeout),
            "Second subscribed queue did not receive the scheduled event");

        var order1 = Assert.IsType<OrderCreated>(Assert.Single(recorder1.Messages));
        Assert.Equal("ORD-SCHED-MULTI", order1.OrderId);
        var order2 = Assert.IsType<OrderCreated>(Assert.Single(recorder2.Messages));
        Assert.Equal("ORD-SCHED-MULTI", order2.OrderId);
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_Should_PreventDeliveryToAllQueues_When_TokenHasMultipleIds()
    {
        // arrange
        var recorder1 = new MessageRecorder();
        var recorder2 = new MessageRecorder();
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddKeyedSingleton("r1", recorder1)
            .AddKeyedSingleton("r2", recorder2)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedScheduledHandler1>()
            .AddEventHandler<OrderCreatedScheduledHandler2>()
            .AddPostgres(t => t.ConnectionString(db.ConnectionString))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var scheduledTime = TimeProvider.System.GetUtcNow().AddSeconds(5);

        var result = await messageBus.SchedulePublishAsync(
            new OrderCreated { OrderId = "ORD-SCHED-MULTI-CANCEL" },
            scheduledTime,
            CancellationToken.None);

        Assert.NotNull(result.Token);
        Assert.Contains(',', result.Token);

        // act
        var cancelled = await messageBus.CancelScheduledMessageAsync(result.Token!, CancellationToken.None);

        // assert
        Assert.True(cancelled);

        var wait1 = recorder1.WaitAsync(TimeSpan.FromSeconds(7));
        var wait2 = recorder2.WaitAsync(TimeSpan.FromSeconds(7));

        Assert.False(await wait1, "First subscribed queue should not have received a cancelled scheduled message");
        Assert.False(await wait2, "Second subscribed queue should not have received a cancelled scheduled message");
        Assert.Empty(recorder1.Messages);
        Assert.Empty(recorder2.Messages);
    }

    [Fact]
    public async Task SchedulePublishAsync_Should_NotThrowAndDeliverNothing_When_TopicHasNoSubscribedQueues()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddMessageBus()
            .AddPostgres(t => t.ConnectionString(db.ConnectionString))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var scheduledTime = TimeProvider.System.GetUtcNow().AddSeconds(2);

        // act
        var exception = await Record.ExceptionAsync(
            () => messageBus.SchedulePublishAsync(
                new OrderCreated { OrderId = "ORD-SCHED-NO-SUBSCRIBERS" },
                scheduledTime,
                CancellationToken.None).AsTask());

        // assert
        Assert.Null(exception);

        // wait past the scheduled time so a wrongly delivered message would have shown up by now
        await Task.Delay(TimeSpan.FromSeconds(4), TestContext.Current.CancellationToken);

        await using var connection = new NpgsqlConnection(db.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM mocha_message";
        var count = (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
        Assert.Equal(0, count);
    }

    public sealed class ProcessPaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }

    public sealed class OrderCreatedScheduledHandler1([FromKeyedServices("r1")] MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class OrderCreatedScheduledHandler2([FromKeyedServices("r2")] MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }
}
