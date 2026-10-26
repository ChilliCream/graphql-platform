using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Transactions;
using CookieCrumble;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Transport.InMemory;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class DbContextOutboxSchedulingTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DispatchAsync_Should_PersistWithBusinessChanges_When_OptionsSpecifyScheduledTime(bool send, bool synchronous)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
        var scheduledTime = new DateTimeOffset(2050, 1, 1, 0, 0, 0, TimeSpan.Zero);
        db.Orders.Add(new Order { Id = 1 });

        // act
        if (send)
        {
            await db.Outbox.SendAsync(
                new ScheduledCommand("scheduled"), new SendOptions { ScheduledTime = scheduledTime }, TestToken);
        }
        else
        {
            await db.Outbox.PublishAsync(
                new ScheduledEvent("scheduled"), new PublishOptions { ScheduledTime = scheduledTime }, TestToken);
        }

        var beforeSave = await ReadCountsAsync(connectionString);
        if (synchronous)
        {
            db.SaveChanges();
        }
        else
        {
            await db.SaveChangesAsync(TestToken);
        }

        await using var verification = provider.CreateAsyncScope();
        var saved = await verification.ServiceProvider.GetRequiredService<SchedulingDbContext>()
            .Set<OutboxMessage>().AsNoTracking().SingleAsync(TestToken);
        using (saved)
        {
            var envelope = MessageEnvelopeReader.Parse(Encoding.UTF8.GetBytes(saved.Envelope.RootElement.GetRawText()));

            // assert
            new
            {
                BeforeSave = beforeSave,
                AfterSave = await ReadCountsAsync(connectionString),
                TimeMatches = envelope.ScheduledTime == scheduledTime,
                KindMatches = envelope.Headers!.Get(MessageHeaders.MessageKind)
                    == (send ? MessageKind.Send : MessageKind.Publish),
                Payload = JsonSerializer.Deserialize<ScheduledEvent>(envelope.Body.Span)!.Payload
            }.MatchInlineSnapshot(
                """
                {
                  "BeforeSave": "orders=0,outbox=0",
                  "AfterSave": "orders=1,outbox=1",
                  "TimeMatches": true,
                  "KindMatches": true,
                  "Payload": "scheduled"
                }
                """);
        }
    }

    [Fact]
    public async Task PublishAsync_Should_DiscardOnlyFailedMessage_When_MiddlewareThrowsAfterNext()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var fail = false;
        var middleware = new DispatchMiddlewareConfiguration((_, next) => async context =>
        {
            await next(context);
            if (fail)
            {
                throw new InvalidOperationException("after-next");
            }
        }, "FailAfterNext");
        await using var provider = await CreateProviderAsync(connectionString, middleware: middleware);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
        await db.Outbox.PublishAsync(new ScheduledEvent("keep"), TestToken);

        // act
        fail = true;
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.Outbox.PublishAsync(
                new ScheduledEvent("discard"), new PublishOptions { ScheduledTime = DateTimeOffset.UtcNow }, TestToken));
        await db.SaveChangesAsync(TestToken);

        // assert
        new
        {
            Error = exception.Message,
            Rows = await ReadCountsAsync(connectionString)
        }.MatchInlineSnapshot(
            """
            {
              "Error": "after-next",
              "Rows": "orders=0,outbox=1"
            }
            """);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_RetainScheduledMessage_When_SaveFails()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
        db.Orders.Add(new Order { Id = 1 });
        await db.SaveChangesAsync(TestToken);
        db.ChangeTracker.Clear();
        var duplicate = db.Orders.Add(new Order { Id = 1 });
        await db.Outbox.PublishAsync(
            new ScheduledEvent("retry"), new PublishOptions { ScheduledTime = DateTimeOffset.UtcNow }, TestToken);
        var staged = db.ChangeTracker.Entries<OutboxMessage>().Single().Entity;
        var id = staged.Id;

        // act
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(TestToken));
        var afterFailure = await ReadCountsAsync(connectionString);
        var payloadSurvived = staged.Envelope.RootElement.ValueKind == JsonValueKind.Object;
        duplicate.State = EntityState.Detached;
        await db.SaveChangesAsync(TestToken);
        var persistedId = await db.Set<OutboxMessage>().AsNoTracking().Select(message => message.Id).SingleAsync(TestToken);

        // assert
        new
        {
            AfterFailure = afterFailure,
            PayloadSurvived = payloadSurvived,
            SameMessage = persistedId == id,
            AfterRetry = await ReadCountsAsync(connectionString)
        }.MatchInlineSnapshot(
            """
            {
              "AfterFailure": "orders=1,outbox=0",
              "PayloadSurvived": true,
              "SameMessage": true,
              "AfterRetry": "orders=1,outbox=1"
            }
            """);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_RetryScheduledMessage_When_TransactionRollsBackBeforeAcceptance()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
        db.Orders.Add(new Order { Id = 1 });
        await db.Outbox.PublishAsync(
            new ScheduledEvent("rollback"), new PublishOptions { ScheduledTime = DateTimeOffset.UtcNow }, TestToken);
        var entry = db.ChangeTracker.Entries<OutboxMessage>().Single();

        // act
        string beforeCommit;
        await using (var transaction = await db.Database.BeginTransactionAsync(TestToken))
        {
            await db.SaveChangesAsync(acceptAllChangesOnSuccess: false, TestToken);
            beforeCommit = await ReadCountsAsync(connectionString);
            await transaction.RollbackAsync(TestToken);
        }

        var retained = entry.State == EntityState.Added && entry.Entity.Envelope.RootElement.ValueKind == JsonValueKind.Object;
        await db.SaveChangesAsync(TestToken);

        // assert
        new
        {
            BeforeCommit = beforeCommit,
            Retained = retained,
            AfterRetry = await ReadCountsAsync(connectionString)
        }.MatchInlineSnapshot(
            """
            {
              "BeforeCommit": "orders=0,outbox=0",
              "Retained": true,
              "AfterRetry": "orders=1,outbox=1"
            }
            """);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_SignalAfterCompletion_When_AmbientTransactionCommits()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var signal = new VisibilitySignal(connectionString);
        await using var provider = await CreateProviderAsync(connectionString, signal: signal);
        signal.Events.Clear();

        // act
        using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
            await db.Outbox.PublishAsync(
                new ScheduledEvent("ambient"), new PublishOptions { ScheduledTime = DateTimeOffset.UtcNow }, TestToken);
            await db.SaveChangesAsync(TestToken);
            signal.Events.Enqueue("before-complete");
            transaction.Complete();
        }

        // assert
        signal.Events.Distinct().ToArray().MatchInlineSnapshot(
            """
            [
              "before-complete",
              "notify(rows=1)"
            ]
            """);
    }

    [Fact]
    public async Task DisposeAsync_Should_DiscardScheduledMessages_When_PooledContextReturnsWithoutSaving()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, pooled: true);
        SchedulingDbContext first;
        JsonDocument document;
        await using (var scope = provider.CreateAsyncScope())
        {
            first = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
            await first.Outbox.PublishAsync(
                new ScheduledEvent("discard"), new PublishOptions { ScheduledTime = DateTimeOffset.UtcNow }, TestToken);
            document = first.ChangeTracker.Entries<OutboxMessage>().Single().Entity.Envelope;
        }

        // act
        await using var nextScope = provider.CreateAsyncScope();
        var next = nextScope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
        await next.SaveChangesAsync(TestToken);

        // assert
        Assert.Same(first, next);
        Assert.Throws<ObjectDisposedException>(() => document.RootElement.ValueKind);
        Assert.Empty(next.ChangeTracker.Entries());
        Assert.Equal("orders=0,outbox=0", await ReadCountsAsync(connectionString));
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverAfterCommit_When_InMemorySchedulingIsUsed()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
        var recorder = provider.GetRequiredService<Recorder>();

        // act
        await db.Outbox.PublishAsync(
            new ScheduledEvent("delivered"),
            new PublishOptions { ScheduledTime = DateTimeOffset.UtcNow.AddSeconds(-1) },
            TestToken);
        var beforeSave = await ReadCountsAsync(connectionString);
        await db.SaveChangesAsync(TestToken);
        var afterSave = await ReadCountsAsync(connectionString);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var delivered = await recorder.Delivered.Task.WaitAsync(TimeSpan.FromSeconds(15), TestToken);

        // assert
        new { BeforeSave = beforeSave, AfterSave = afterSave, Delivered = delivered }.MatchInlineSnapshot(
            """
            {
              "BeforeSave": "orders=0,outbox=0",
              "AfterSave": "orders=0,outbox=1",
              "Delivered": "delivered"
            }
            """);
    }

    private static async Task<ServiceProvider> CreateProviderAsync(
        string connectionString,
        bool pooled = false,
        DispatchMiddlewareConfiguration? middleware = null,
        IOutboxSignal? signal = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Recorder>();
        services.AddSingleton<IOutboxSignal>(signal ?? new StubOutboxSignal());

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<SchedulingDbContext>(ef => ef.UsePostgresOutbox());
        if (pooled)
        {
            services.AddDbContextPool<SchedulingDbContext>(options => options.UseTestNpgsql(connectionString), poolSize: 1);
        }
        else
        {
            services.AddDbContext<SchedulingDbContext>(options => options.UseTestNpgsql(connectionString));
        }

        builder.AddEventHandler<ScheduledEventHandler>();
        builder.AddRequestHandler<ScheduledCommandHandler>();
        builder.AddInMemory();
        if (middleware is not null)
        {
            builder.ConfigureMessageBus(bus => bus.UseDispatch(middleware));
        }

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SchedulingDbContext>().Database.EnsureCreatedAsync(TestToken);
        if (pooled)
        {
            scope.ServiceProvider.GetRequiredService<IMessageOutbox>();
        }

        return provider;
    }

    private static async Task<string> ReadCountsAsync(string connectionString)
    {
        var settings = new NpgsqlConnectionStringBuilder(connectionString) { Enlist = false };
        await using var connection = new NpgsqlConnection(settings.ConnectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT (SELECT COUNT(*) FROM orders),
                   (SELECT COUNT(*) FROM outbox_messages)
            """;
        await using var reader = await command.ExecuteReaderAsync(TestToken);
        await reader.ReadAsync(TestToken);
        return $"orders={reader.GetInt64(0)},outbox={reader.GetInt64(1)}";
    }

    public sealed class SchedulingDbContext(DbContextOptions<SchedulingDbContext> options) : DbContext(options)
    {
        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddPostgresOutbox();
            modelBuilder.Entity<Order>().ToTable("orders").Property(order => order.Id).ValueGeneratedNever();
        }
    }

    public sealed class Order
    {
        public int Id { get; set; }
    }

    public sealed record ScheduledEvent(string Payload);

    public sealed record ScheduledCommand(string Payload) : IEventRequest;

    public sealed class Recorder
    {
        public TaskCompletionSource<string> Delivered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class ScheduledEventHandler(Recorder recorder) : IEventHandler<ScheduledEvent>
    {
        public ValueTask HandleAsync(ScheduledEvent message, CancellationToken cancellationToken)
        {
            recorder.Delivered.TrySetResult(message.Payload);
            return default;
        }
    }

    public sealed class ScheduledCommandHandler(Recorder recorder) : IEventRequestHandler<ScheduledCommand>
    {
        public ValueTask HandleAsync(ScheduledCommand message, CancellationToken cancellationToken)
        {
            recorder.Delivered.TrySetResult(message.Payload);
            return default;
        }
    }

    private sealed class VisibilitySignal(string connectionString) : IOutboxSignal
    {
        public ConcurrentQueue<string> Events { get; } = new();

        public void Set()
        {
            var settings = new NpgsqlConnectionStringBuilder(connectionString) { Enlist = false };
            using var connection = new NpgsqlConnection(settings.ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM outbox_messages";
            Events.Enqueue($"notify(rows={command.ExecuteScalar()})");
        }

        public Task WaitAsync(CancellationToken cancellationToken)
            => Task.Delay(Timeout.Infinite, cancellationToken);
    }
}
