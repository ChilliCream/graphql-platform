using CookieCrumble;
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
    public async Task DispatchAsync_Should_UseNativeSchedulerAfterCommit_When_OptionsSpecifyScheduledTime(bool send)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync<TestDbContext>(connectionString, Scheduler.Postgres);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var recorder = provider.GetRequiredService<Recorder>();
        var scheduledTime = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 5);

        // act
        if (send)
        {
            await db.Outbox(bus).SendAsync(
                new SendReminderCommand("scheduled"), new SendOptions { ScheduledTime = scheduledTime }, TestToken);
        }
        else
        {
            await db.Outbox(bus).PublishAsync(
                new ReminderDueEvent("scheduled"), new PublishOptions { ScheduledTime = scheduledTime }, TestToken);
        }

        var beforeSave = await ReadCountsAsync(connectionString, Scheduler.Postgres);
        await db.SaveChangesAsync(TestToken);
        var afterSave = await ReadCountsAsync(connectionString, Scheduler.Postgres);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        await WaitForEmptyOutboxAsync(connectionString);
        var afterHandoff = await ReadCountsAsync(connectionString, Scheduler.Postgres);
        var storedTime = await ReadScheduledTimeAsync(connectionString, "mocha_message");
        var deliveredBeforeDue = recorder.Delivered.Task.IsCompleted;
        var delivered = await recorder.Delivered.Task.WaitAsync(s_timeout, TestToken);

        // assert
        new
        {
            BeforeSave = beforeSave,
            AfterSave = afterSave,
            AfterHandoff = afterHandoff,
            ScheduledTimeMatches = storedTime == scheduledTime,
            DeliveredBeforeDue = deliveredBeforeDue,
            DeliveredAfterDue = delivered.ReceivedAt >= scheduledTime,
            delivered.Payload,
            recorder.MiddlewareCalls,
            NoFallbackTable = await HasNoFallbackTableAsync(connectionString)
        }.MatchInlineSnapshot(
            """
            {
              "BeforeSave": "outbox=0,scheduled=0",
              "AfterSave": "outbox=1,scheduled=0",
              "AfterHandoff": "outbox=0,scheduled=1",
              "ScheduledTimeMatches": true,
              "DeliveredBeforeDue": false,
              "DeliveredAfterDue": true,
              "Payload": "scheduled",
              "MiddlewareCalls": 1,
              "NoFallbackTable": true
            }
            """);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_NotEnqueueNativeMessage_When_TransactionRollsBack()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync<TestDbContext>(connectionString, Scheduler.Postgres);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var recorder = provider.GetRequiredService<Recorder>();

        // act
        await db.Outbox(bus).PublishAsync(
            new ReminderDueEvent("rolled-back"),
            new PublishOptions { ScheduledTime = DateTimeOffset.UtcNow.AddSeconds(-1) },
            TestToken);
        var beforeSave = await ReadCountsAsync(connectionString, Scheduler.Postgres);
        string beforeCommit;
        await using (var transaction = await db.Database.BeginTransactionAsync(TestToken))
        {
            await db.SaveChangesAsync(TestToken);
            beforeCommit = await ReadCountsAsync(connectionString, Scheduler.Postgres);
            await transaction.RollbackAsync(TestToken);
        }

        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);

        // assert
        new
        {
            BeforeSave = beforeSave,
            BeforeCommit = beforeCommit,
            AfterRollback = await ReadCountsAsync(connectionString, Scheduler.Postgres),
            Delivered = recorder.Delivered.Task.IsCompleted,
            recorder.MiddlewareCalls
        }.MatchInlineSnapshot(
            """
            {
              "BeforeSave": "outbox=0,scheduled=0",
              "BeforeCommit": "outbox=0,scheduled=0",
              "AfterRollback": "outbox=0,scheduled=0",
              "Delivered": false,
              "MiddlewareCalls": 1
            }
            """);
    }

    [Fact]
    public async Task ProcessAsync_Should_RetainAndRetryOutboxMessage_When_NativeSchedulerHandoffFails()
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
            new SendOptions { ScheduledTime = DateTimeOffset.UtcNow.AddSeconds(-1) },
            TestToken);
        var id = db.ChangeTracker.Entries<OutboxMessage>().Single().Entity.Id;
        await db.SaveChangesAsync(TestToken);
        await ExecuteAsync(connectionString,
            "ALTER TABLE mocha_message ADD CONSTRAINT reject_handoff CHECK (false) NOT VALID");

        // act
        var processor = provider.GetRequiredKeyedService<PostgresOutboxProcessor>(typeof(TestDbContext));
        var exception = await Assert.ThrowsAsync<PostgresException>(() => processor.ProcessAsync(TestToken));
        var afterFailure = await ReadCountsAsync(connectionString, Scheduler.Postgres);
        using var retained = await db.Set<OutboxMessage>().AsNoTracking().SingleAsync(TestToken);
        await ExecuteAsync(connectionString, "ALTER TABLE mocha_message DROP CONSTRAINT reject_handoff");
        await ExecuteAsync(connectionString, "UPDATE outbox_messages SET created_at = NOW()");
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var delivered = await recorder.Delivered.Task.WaitAsync(s_timeout, TestToken);
        await WaitForEmptyOutboxAsync(connectionString);

        // assert
        new
        {
            Error = exception.SqlState,
            AfterFailure = afterFailure,
            SameMessage = retained.Id == id,
            retained.TimesSent,
            delivered.Payload,
            recorder.MiddlewareCalls,
            OutboxEmpty = !await db.Set<OutboxMessage>().AnyAsync(TestToken)
        }.MatchInlineSnapshot(
            """
            {
              "Error": "23514",
              "AfterFailure": "outbox=1,scheduled=0",
              "SameMessage": true,
              "TimesSent": 1,
              "Payload": "retried",
              "MiddlewareCalls": 1,
              "OutboxEmpty": true
            }
            """);
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
    public async Task PublishAsync_Should_UseSeparateSchedulerAfterCommit_When_FallbackIsConfigured()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync<FallbackDbContext>(connectionString, Scheduler.Fallback);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<FallbackDbContext>();
        var recorder = provider.GetRequiredService<Recorder>();
        var scheduledTime = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 5);

        // act
        await db.Outbox(bus).PublishAsync(
            new ReminderDueEvent("fallback"), new PublishOptions { ScheduledTime = scheduledTime }, TestToken);
        var beforeSave = await ReadCountsAsync(connectionString, Scheduler.Fallback);
        await db.SaveChangesAsync(TestToken);
        var afterSave = await ReadCountsAsync(connectionString, Scheduler.Fallback);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        await WaitForEmptyOutboxAsync(connectionString);
        var afterHandoff = await ReadCountsAsync(connectionString, Scheduler.Fallback);
        var storedTime = await ReadScheduledTimeAsync(connectionString, "scheduled_messages");
        var deliveredBeforeDue = recorder.Delivered.Task.IsCompleted;
        var delivered = await recorder.Delivered.Task.WaitAsync(s_timeout, TestToken);

        // assert
        new
        {
            BeforeSave = beforeSave,
            AfterSave = afterSave,
            AfterHandoff = afterHandoff,
            ScheduledTimeMatches = storedTime == scheduledTime,
            DeliveredBeforeDue = deliveredBeforeDue,
            DeliveredAfterDue = delivered.ReceivedAt >= scheduledTime,
            delivered.Payload,
            recorder.MiddlewareCalls
        }.MatchInlineSnapshot(
            """
            {
              "BeforeSave": "outbox=0,scheduled=0",
              "AfterSave": "outbox=1,scheduled=0",
              "AfterHandoff": "outbox=0,scheduled=1",
              "ScheduledTimeMatches": true,
              "DeliveredBeforeDue": false,
              "DeliveredAfterDue": true,
              "Payload": "fallback",
              "MiddlewareCalls": 1
            }
            """);
    }

    private static async Task<ServiceProvider> CreateProviderAsync<TContext>(string connectionString, Scheduler scheduler)
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
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<TContext>().Database.EnsureCreatedAsync(TestToken);
        }

        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);
        return provider;
    }

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
