#if NET9_0_OR_GREATER
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Mocha.Scheduling;
using Mocha.Transport.InMemory;
using Mocha.Transport.Postgres;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxSchedulingTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(20);

    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DispatchAsync_Should_HandOffToNativeSchedulerAfterCommit_When_ScheduledTimeIsSpecified(bool send)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync<TestDbContext>(connectionString, Scheduler.Postgres);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var scheduledTime = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600);

        // act
        await ScheduleAsync(db.Outbox(bus), send, scheduledTime);
        var beforeSave = await ReadCountsAsync(connectionString, Scheduler.Postgres);
        await db.SaveChangesAsync(TestToken);
        var afterSave = await ReadCountsAsync(connectionString, Scheduler.Postgres);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        await WaitForEmptyOutboxAsync(connectionString);

        // assert
        Assert.Equal("outbox=0,scheduled=0", beforeSave);
        Assert.Equal("outbox=1,scheduled=0", afterSave);
        Assert.Equal("outbox=0,scheduled=1", await ReadCountsAsync(connectionString, Scheduler.Postgres));
        Assert.Equal(scheduledTime, await ReadScheduledTimeAsync(connectionString, "mocha_message"));
        Assert.True(await HasNoFallbackTableAsync(connectionString));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DispatchAsync_Should_DeliverAtScheduledTime_When_NativeSchedulerIsUsed(bool send)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync<TestDbContext>(connectionString, Scheduler.Postgres);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var recorder = provider.GetRequiredService<Recorder>();
        var scheduledTime = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 5);
        await ScheduleAsync(db.Outbox(bus), send, scheduledTime);
        await db.SaveChangesAsync(TestToken);

        // act
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var delivered = await recorder.Delivered.Task.WaitAsync(s_timeout, TestToken);

        // assert
        Assert.InRange(delivered.ReceivedAt, scheduledTime, DateTimeOffset.MaxValue);
        Assert.Equal("scheduled", delivered.Payload);
        Assert.Equal(1, recorder.MiddlewareCalls);
    }

    [Fact]
    public async Task PublishAsync_Should_HandOffToScheduler_When_MiddlewarePresetsSentAt()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync<TestDbContext>(
            connectionString,
            Scheduler.Postgres,
            bus => bus.UseDispatch(new DispatchMiddlewareConfiguration(
                static (_, next) => context =>
                {
                    context.SentAt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
                    return next(context);
                },
                "PresetSentAt")));
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var recorder = provider.GetRequiredService<Recorder>();
        var scheduledTime = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600);
        await db.Outbox(bus).PublishAsync(
            new ReminderDueEvent("scheduled"), new PublishOptions { ScheduledTime = scheduledTime }, TestToken);
        await db.SaveChangesAsync(TestToken);

        // act
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        await WaitForEmptyOutboxAsync(connectionString);

        // assert
        Assert.Equal("outbox=0,scheduled=1", await ReadCountsAsync(connectionString, Scheduler.Postgres));
        Assert.Equal(scheduledTime, await ReadScheduledTimeAsync(connectionString, "mocha_message"));
        Assert.False(recorder.Delivered.Task.IsCompleted);
    }

    [Fact]
    public async Task ProcessAsync_Should_RetainOutboxMessage_When_NativeSchedulerHandoffFails()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync<TestDbContext>(connectionString, Scheduler.Postgres);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await db.Outbox(bus).SendAsync(
            new SendReminderCommand("retried"),
            new SendOptions { ScheduledTime = DateTimeOffset.UtcNow.AddHours(1) },
            TestToken);
        var id = db.ChangeTracker.Entries<OutboxMessage>().Single().Entity.Id;
        await db.SaveChangesAsync(TestToken);
        await ExecuteAsync(connectionString,
            "ALTER TABLE mocha_message ADD CONSTRAINT reject_handoff CHECK (false) NOT VALID");
        var processor = provider.GetRequiredKeyedService<PostgresOutboxProcessor>(typeof(TestDbContext));

        // act
        var exception = await Assert.ThrowsAsync<PostgresException>(() => processor.ProcessAsync(TestToken));
        var retained = await db.Set<OutboxMessage>().AsNoTracking().SingleAsync(TestToken);

        // assert
        Assert.Equal("23514", exception.SqlState);
        Assert.Equal("outbox=1,scheduled=0", await ReadCountsAsync(connectionString, Scheduler.Postgres));
        Assert.Equal(id, retained.Id);
        Assert.Equal(1, retained.TimesSent);
    }

    [Fact]
    public async Task ProcessAsync_Should_DeliverOutboxMessage_When_NativeSchedulerHandoffIsRetried()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync<TestDbContext>(connectionString, Scheduler.Postgres);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var recorder = provider.GetRequiredService<Recorder>();
        await db.Outbox(bus).SendAsync(
            new SendReminderCommand("retried"),
            new SendOptions { ScheduledTime = DateTimeOffset.UtcNow.AddSeconds(5) },
            TestToken);
        await db.SaveChangesAsync(TestToken);
        await ExecuteAsync(connectionString,
            "ALTER TABLE mocha_message ADD CONSTRAINT reject_handoff CHECK (false) NOT VALID");
        var processor = provider.GetRequiredKeyedService<PostgresOutboxProcessor>(typeof(TestDbContext));
        await Assert.ThrowsAsync<PostgresException>(() => processor.ProcessAsync(TestToken));
        await ExecuteAsync(connectionString, "ALTER TABLE mocha_message DROP CONSTRAINT reject_handoff");
        await ExecuteAsync(connectionString, "UPDATE outbox_messages SET created_at = NOW()");

        // act
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var delivered = await recorder.Delivered.Task.WaitAsync(s_timeout, TestToken);
        await WaitForEmptyOutboxAsync(connectionString);

        // assert
        Assert.Equal("retried", delivered.Payload);
        Assert.Equal(1, recorder.MiddlewareCalls);
        Assert.False(await db.Set<OutboxMessage>().AnyAsync(TestToken));
    }

    [Fact]
    public async Task PublishAsync_Should_FailBeforeStaging_When_TransportHasNoScheduler()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync<TestDbContext>(connectionString, Scheduler.None);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // act
        var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await db.Outbox(bus).PublishAsync(
                new ReminderDueEvent("unsupported"),
                new PublishOptions { ScheduledTime = DateTimeOffset.UtcNow },
                TestToken));

        // assert
        Assert.Equal(
            "Scheduled dispatch is not supported for transport "
            + "'Mocha.Transport.InMemory.InMemoryMessagingTransport'. Register a transport-specific scheduled "
            + "message store or configure a fallback scheduler.",
            exception.Message);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Equal(0, await db.Set<OutboxMessage>().CountAsync(TestToken));
    }

    [Fact]
    public async Task PublishAsync_Should_HandOffToFallbackSchedulerAfterCommit_When_FallbackIsConfigured()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync<FallbackDbContext>(connectionString, Scheduler.Fallback);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<FallbackDbContext>();
        var scheduledTime = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600);

        // act
        await db.Outbox(bus).PublishAsync(
            new ReminderDueEvent("fallback"), new PublishOptions { ScheduledTime = scheduledTime }, TestToken);
        var beforeSave = await ReadCountsAsync(connectionString, Scheduler.Fallback);
        await db.SaveChangesAsync(TestToken);
        var afterSave = await ReadCountsAsync(connectionString, Scheduler.Fallback);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        await WaitForEmptyOutboxAsync(connectionString);

        // assert
        Assert.Equal("outbox=0,scheduled=0", beforeSave);
        Assert.Equal("outbox=1,scheduled=0", afterSave);
        Assert.Equal("outbox=0,scheduled=1", await ReadCountsAsync(connectionString, Scheduler.Fallback));
        Assert.Equal(scheduledTime, await ReadScheduledTimeAsync(connectionString, "scheduled_messages"));
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverAtScheduledTime_When_FallbackIsConfigured()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync<FallbackDbContext>(connectionString, Scheduler.Fallback);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<FallbackDbContext>();
        var recorder = provider.GetRequiredService<Recorder>();
        var scheduledTime = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 5);
        await db.Outbox(bus).PublishAsync(
            new ReminderDueEvent("fallback"), new PublishOptions { ScheduledTime = scheduledTime }, TestToken);
        await db.SaveChangesAsync(TestToken);

        // act
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var delivered = await recorder.Delivered.Task.WaitAsync(s_timeout, TestToken);

        // assert
        Assert.InRange(delivered.ReceivedAt, scheduledTime, DateTimeOffset.MaxValue);
        Assert.Equal("fallback", delivered.Payload);
        Assert.Equal(1, recorder.MiddlewareCalls);
    }

    private static async Task<ServiceProvider> CreateProviderAsync<TContext>(
        string connectionString,
        Scheduler scheduler,
        Action<IMessageBusBuilder>? configureBus = null)
        where TContext : DbContext
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Recorder>();
        services.AddSingleton<IOutboxSignal, StubOutboxSignal>();
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TContext>(ef =>
        {
            ef.UsePostgresOutbox();
            if (scheduler == Scheduler.Fallback)
            {
                ef.UsePostgresScheduling();
            }
        });
        services.AddDbContext<TContext>(options => options.UseTestNpgsql(connectionString));
        builder.AddEventHandler<ReminderDueEventHandler>();
        builder.AddRequestHandler<SendReminderCommandHandler>();
        if (scheduler == Scheduler.Postgres)
        {
            builder.AddPostgres(transport => transport.ConnectionString(connectionString));
        }
        else
        {
            builder.ConfigureMessageBus(bus => bus.AddTransport(new InMemoryMessagingTransport(static _ => { })));
        }

        builder.ConfigureMessageBus(bus => bus.UseDispatch(new DispatchMiddlewareConfiguration(
            (_, next) => context =>
            {
                context.Services.GetRequiredService<Recorder>().MiddlewareCalls++;
                return next(context);
            },
            "CountOriginalDispatch")));
        if (configureBus is not null)
        {
            builder.ConfigureMessageBus(configureBus);
        }

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<TContext>().Database.EnsureCreatedAsync(TestToken);
        }

        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);
        return provider;
    }

    private static ValueTask ScheduleAsync(DbContextOutbox outbox, bool send, DateTimeOffset scheduledTime)
        => send
            ? outbox.SendAsync(
                new SendReminderCommand("scheduled"), new SendOptions { ScheduledTime = scheduledTime }, TestToken)
            : outbox.PublishAsync(
                new ReminderDueEvent("scheduled"), new PublishOptions { ScheduledTime = scheduledTime }, TestToken);

    private static async Task<string> ReadCountsAsync(string connectionString, Scheduler scheduler)
    {
        var settings = new NpgsqlConnectionStringBuilder(connectionString) { Enlist = false };
        await using var connection = new NpgsqlConnection(settings.ConnectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();
        var table = scheduler == Scheduler.Postgres ? "mocha_message" : "scheduled_messages";
        command.CommandText = $"SELECT (SELECT COUNT(*) FROM outbox_messages), (SELECT COUNT(*) FROM {table})";
        await using var reader = await command.ExecuteReaderAsync(TestToken);
        await reader.ReadAsync(TestToken);
        return $"outbox={reader.GetInt64(0)},scheduled={reader.GetInt64(1)}";
    }

    private static async Task<DateTimeOffset> ReadScheduledTimeAsync(string connectionString, string table)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT scheduled_time FROM {table}";
        return new DateTimeOffset((DateTime)(await command.ExecuteScalarAsync(TestToken))!);
    }

    private static async Task<bool> HasNoFallbackTableAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT to_regclass('scheduled_messages') IS NULL";
        return (bool)(await command.ExecuteScalarAsync(TestToken))!;
    }

    private static async Task WaitForEmptyOutboxAsync(string connectionString)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        cancellation.CancelAfter(s_timeout);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellation.Token);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM outbox_messages";
        while ((long)(await command.ExecuteScalarAsync(cancellation.Token))! != 0)
        {
            await Task.Delay(20, cancellation.Token);
        }
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(TestToken);
    }

    public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.AddPostgresOutbox();
    }

    public sealed class FallbackDbContext(DbContextOptions<FallbackDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddPostgresOutbox();
            modelBuilder.AddPostgresScheduledMessages();
        }
    }

    public sealed record ReminderDueEvent(string Payload);

    public sealed record SendReminderCommand(string Payload) : IEventRequest;

    public sealed record Delivery(string Payload, DateTimeOffset ReceivedAt);

    public sealed class Recorder
    {
        public int MiddlewareCalls { get; set; }

        public TaskCompletionSource<Delivery> Delivered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class ReminderDueEventHandler(Recorder recorder) : IEventHandler<ReminderDueEvent>
    {
        public ValueTask HandleAsync(ReminderDueEvent message, CancellationToken cancellationToken)
        {
            recorder.Delivered.TrySetResult(new Delivery(message.Payload, DateTimeOffset.UtcNow));
            return default;
        }
    }

    public sealed class SendReminderCommandHandler(Recorder recorder) : IEventRequestHandler<SendReminderCommand>
    {
        public ValueTask HandleAsync(SendReminderCommand message, CancellationToken cancellationToken)
        {
            recorder.Delivered.TrySetResult(new Delivery(message.Payload, DateTimeOffset.UtcNow));
            return default;
        }
    }

    private enum Scheduler
    {
        None,
        Postgres,
        Fallback
    }
}
#endif
