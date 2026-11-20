#if NET9_0_OR_GREATER
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Threading.Channels;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Mocha.Transport.InMemory;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresDbContextOutboxTransactionTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task OutboxPublishAsync_Should_DeliverBothSavesAfterCommit_When_TransactionContainsMultipleSaves()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var provider = await CreateProviderAsync(
            connectionString,
            TimeSpan.FromMilliseconds(100),
            TimeProvider.System,
            static _ => { });
        var polls = ObserveOutboxPolls(provider);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        await polls.ReadAsync(TestToken).AsTask().WaitAsync(s_timeout, TestToken);
        var recorder = provider.GetRequiredService<MessageRecorder>();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await using var transaction = await db.Database.BeginTransactionAsync(TestToken);

        // act
        db.Orders.Add(new Order { Id = 1, Attempt = 1 });
        await db.Outbox(bus).PublishAsync(new OrderPlaced(1, 1), TestToken);
        await db.SaveChangesAsync(TestToken);
        var afterFirstSave = await CountRowsAsync(provider);

        db.Orders.Add(new Order { Id = 2, Attempt = 1 });
        await db.Outbox(bus).PublishAsync(new OrderPlaced(2, 1), TestToken);
        await db.SaveChangesAsync(TestToken);
        var afterSecondSave = await CountRowsAsync(provider);

        await WaitForPollStartedAfterNowAsync(polls);
        var deliveredBeforeCommit = recorder.Messages.ToArray();
        await transaction.CommitAsync(TestToken);
        await recorder.WaitAsync(2, TestToken);
        await WaitForEmptyOutboxAsync(provider);
        var orderIds = await db.Orders.OrderBy(o => o.Id).Select(o => o.Id).ToArrayAsync(TestToken);

        // assert
        Assert.Equal((0, 0), afterFirstSave);
        Assert.Equal((0, 0), afterSecondSave);
        Assert.Empty(deliveredBeforeCommit);
        Assert.Equal([1, 2], orderIds);
        Assert.Equal([new OrderPlaced(1, 1), new OrderPlaced(2, 1)], recorder.Messages.OrderBy(m => m.OrderId));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_RollBackFailedAttempt_When_ConsumerSavesBeforeRetrying()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var provider = await CreateProviderAsync(
            connectionString,
            TimeSpan.FromMinutes(1),
            TimeProvider.System,
            static _ => { });
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var recorder = provider.GetRequiredService<MessageRecorder>();
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();

        // act
        await bus.RequestAsync(new PlaceOrder(1), TestToken).AsTask().WaitAsync(s_timeout, TestToken);
        await recorder.WaitAsync(1, TestToken);
        await WaitForEmptyOutboxAsync(provider);
        var order = await db.Orders.SingleAsync(TestToken);

        // assert
        Assert.Equal(2, recorder.Attempts);
        Assert.Equal((0, 0), recorder.RowsBeforeRetry);
        Assert.Equal(1, order.Id);
        Assert.Equal(2, order.Attempt);
        Assert.Equal([new OrderPlaced(1, 2)], recorder.Messages);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_DeliverAfterCommit_When_TransactionIsCommittedOutsideEntityFramework()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var provider = await CreateProviderAsync(
            connectionString,
            TimeSpan.FromMilliseconds(100),
            TimeProvider.System,
            static _ => { });
        var polls = ObserveOutboxPolls(provider);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        await polls.ReadAsync(TestToken).AsTask().WaitAsync(s_timeout, TestToken);
        var recorder = provider.GetRequiredService<MessageRecorder>();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await db.Database.OpenConnectionAsync(TestToken);
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await using var transaction = await connection.BeginTransactionAsync(TestToken);
        await db.Database.UseTransactionAsync(transaction, TestToken);

        // act
        db.Orders.Add(new Order { Id = 1, Attempt = 1 });
        await db.Outbox(bus).PublishAsync(new OrderPlaced(1, 1), TestToken);
        await db.SaveChangesAsync(TestToken);
        var beforeCommit = await CountRowsAsync(provider);
        while (polls.TryRead(out _))
        {
        }

        await polls.ReadAsync(TestToken).AsTask().WaitAsync(s_timeout, TestToken);
        var deliveredBeforeCommit = recorder.Messages.ToArray();
        await transaction.CommitAsync(TestToken);
        await recorder.WaitAsync(1, TestToken);
        await WaitForEmptyOutboxAsync(provider);
        var afterCommit = await CountRowsAsync(provider);

        // assert
        Assert.Equal((0, 0), beforeCommit);
        Assert.Empty(deliveredBeforeCommit);
        Assert.Equal((1, 0), afterCommit);
        Assert.Equal([new OrderPlaced(1, 1)], recorder.Messages);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_DiscardSave_When_TransactionOutsideEntityFrameworkIsRolledBack()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var provider = await CreateProviderAsync(
            connectionString,
            TimeSpan.FromMilliseconds(100),
            TimeProvider.System,
            static _ => { });
        var polls = ObserveOutboxPolls(provider);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var recorder = provider.GetRequiredService<MessageRecorder>();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await db.Database.OpenConnectionAsync(TestToken);
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await using var transaction = await connection.BeginTransactionAsync(TestToken);
        await db.Database.UseTransactionAsync(transaction, TestToken);

        // act
        db.Orders.Add(new Order { Id = 1, Attempt = 1 });
        await db.Outbox(bus).PublishAsync(new OrderPlaced(1, 1), TestToken);
        await db.SaveChangesAsync(TestToken);
        await transaction.RollbackAsync(TestToken);
        await WaitForPollStartedAfterNowAsync(polls);
        var afterRollback = await CountRowsAsync(provider);

        // assert
        Assert.Equal((0, 0), afterRollback);
        Assert.Empty(recorder.Messages);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_DiscardBothSaves_When_TransactionIsRolledBack()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var provider = await CreateProviderAsync(
            connectionString,
            TimeSpan.FromMilliseconds(100),
            TimeProvider.System,
            static _ => { });
        var polls = ObserveOutboxPolls(provider);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var recorder = provider.GetRequiredService<MessageRecorder>();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await using (var transaction = await db.Database.BeginTransactionAsync(TestToken))
        {
            db.Orders.Add(new Order { Id = 1, Attempt = 1 });
            await db.Outbox(bus).PublishAsync(new OrderPlaced(1, 1), TestToken);
            await db.SaveChangesAsync(TestToken);

            db.Orders.Add(new Order { Id = 2, Attempt = 1 });
            await db.Outbox(bus).PublishAsync(new OrderPlaced(2, 1), TestToken);
            await db.SaveChangesAsync(TestToken);

            await transaction.RollbackAsync(TestToken);
        }

        await WaitForPollStartedAfterNowAsync(polls);
        var afterRollback = await CountRowsAsync(provider);

        // assert
        Assert.Equal((0, 0), afterRollback);
        Assert.Empty(recorder.Messages);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_DiscardSave_When_TransactionScopeIsNotCompleted()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var provider = await CreateProviderAsync(
            connectionString,
            TimeSpan.FromMilliseconds(100),
            TimeProvider.System,
            static _ => { });
        var polls = ObserveOutboxPolls(provider);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var recorder = provider.GetRequiredService<MessageRecorder>();

        // act
        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            db.Orders.Add(new Order { Id = 1, Attempt = 1 });
            await db.Outbox(bus).PublishAsync(new OrderPlaced(1, 1), TestToken);
            await db.SaveChangesAsync(TestToken);
        }

        await WaitForPollStartedAfterNowAsync(polls);
        var afterRollback = await CountRowsAsync(provider);

        // assert
        Assert.Equal((0, 0), afterRollback);
        Assert.Empty(recorder.Messages);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_DeliverAfterCompletion_When_TransactionScopeIsCompleted()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var provider = await CreateProviderAsync(
            connectionString,
            TimeSpan.FromMinutes(1),
            TimeProvider.System,
            static _ => { });
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var recorder = provider.GetRequiredService<MessageRecorder>();

        // act
        using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            db.Orders.Add(new Order { Id = 1, Attempt = 1 });
            await db.Outbox(bus).PublishAsync(new OrderPlaced(1, 1), TestToken);
            await db.SaveChangesAsync(TestToken);
            transaction.Complete();
        }

        await recorder.WaitAsync(1, TestToken);
        await WaitForEmptyOutboxAsync(provider);

        // assert
        Assert.Equal([new OrderPlaced(1, 1)], recorder.Messages);
        Assert.Equal((1, 0), await CountRowsAsync(provider));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_DeliverOnce_When_ExecutionStrategyRetriesTransaction()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var failFirstInsert = new FailFirstOutboxInsertInterceptor();
        var provider = await CreateProviderAsync(
            connectionString,
            TimeSpan.FromMinutes(1),
            TimeProvider.System,
            options => options
                .UseNpgsql(connectionString, o => o.EnableRetryOnFailure(3, TimeSpan.FromMilliseconds(100), null))
                .AddInterceptors(failFirstInsert));
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var recorder = provider.GetRequiredService<MessageRecorder>();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var attempts = 0;
        var messagesBeforeCommit = 0;

        // act
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            attempts++;
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(TestToken);
            db.Orders.Add(new Order { Id = 1, Attempt = attempts });
            await db.Outbox(bus).PublishAsync(new OrderPlaced(1, attempts), TestToken);
            await db.SaveChangesAsync(TestToken);
            messagesBeforeCommit = await db.Set<OutboxMessage>().CountAsync(TestToken);
            await transaction.CommitAsync(TestToken);
        });
        await recorder.WaitAsync(1, TestToken);
        await WaitForEmptyOutboxAsync(provider);
        var orderAttempts = await db.Orders.Select(o => o.Attempt).ToArrayAsync(TestToken);

        // assert
        Assert.Equal(2, attempts);
        Assert.Equal(1, failFirstInsert.Failures);
        Assert.Equal(1, messagesBeforeCommit);
        Assert.Equal([2], orderAttempts);
        Assert.Equal([new OrderPlaced(1, 2)], recorder.Messages);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_DeliverPromptly_When_ApplicationClockIsAheadOfDatabase()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var provider = await CreateProviderAsync(
            connectionString,
            TimeSpan.FromMinutes(1),
            new SkewedTimeProvider(TimeSpan.FromSeconds(30)),
            static _ => { });
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var recorder = provider.GetRequiredService<MessageRecorder>();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        db.Orders.Add(new Order { Id = 1, Attempt = 1 });
        await db.Outbox(bus).PublishAsync(new OrderPlaced(1, 1), TestToken);
        await db.SaveChangesAsync(TestToken);
        await recorder.WaitAsync(1, TestToken).WaitAsync(TimeSpan.FromSeconds(10), TestToken);

        // assert
        Assert.Equal([new OrderPlaced(1, 1)], recorder.Messages);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_DeliverPromptly_When_SaveWritesOnlyTheMessage()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var provider = await CreateProviderAsync(
            connectionString,
            TimeSpan.FromMinutes(10),
            TimeProvider.System,
            static _ => { });
        var polls = ObserveOutboxPolls(provider);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        await polls.ReadAsync(TestToken).AsTask().WaitAsync(s_timeout, TestToken);
        var recorder = provider.GetRequiredService<MessageRecorder>();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await db.Outbox(bus).PublishAsync(new OrderPlaced(1, 1), TestToken);
        await db.SaveChangesAsync(TestToken);
        await recorder.WaitAsync(1, TestToken).WaitAsync(TimeSpan.FromSeconds(5), TestToken);

        // assert
        Assert.Equal([new OrderPlaced(1, 1)], recorder.Messages);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(50 * 24 * 60 * 60)]
    public void UsePostgresOutbox_Should_RejectPollingInterval_When_OutOfRange(int seconds)
    {
        // arrange
        var services = new ServiceCollection();
        services.AddMessageBus().AddEntityFramework<TransactionDbContext>(
            ef => ef.UsePostgresOutbox(options => options.PollingInterval = TimeSpan.FromSeconds(seconds)));
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<PostgresOutboxOptions>>();

        // act
        var exception = Assert.Throws<OptionsValidationException>(
            () => options.Get(typeof(TransactionDbContext).FullName));

        // assert
        Assert.Equal(
            "PostgresOutboxOptions.PollingInterval must be greater than zero and at most 49.17:02:47.2940000.",
            exception.Message);
    }

    private static async Task<ServiceProvider> CreateProviderAsync(
        string connectionString,
        TimeSpan pollingInterval,
        TimeProvider timeProvider,
        Action<DbContextOptionsBuilder> configureContext)
    {
        await using (var db = new TransactionDbContext(new DbContextOptionsBuilder<TransactionDbContext>()
            .UseTestNpgsql(connectionString).Options))
        {
            await db.Database.EnsureCreatedAsync(TestToken);
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<MessageRecorder>();
        services.AddSingleton(timeProvider);
        services.AddDbContext<TransactionDbContext>(options =>
        {
            options.UseTestNpgsql(connectionString);
            configureContext(options);
        });
        services.AddMessageBus()
            .AddEntityFramework<TransactionDbContext>(ef =>
            {
                ef.UsePostgresOutbox(options => options.PollingInterval = pollingInterval);
                ef.UseTransaction();
            })
            .AddRequestHandler<PlaceOrderHandler>()
            .AddEventHandler<OrderPlacedHandler>()
            .AddResilience(policy => policy.On<RetryOrderException>().Retry(1, TimeSpan.Zero, RetryBackoffType.Constant))
            .AddInMemory();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static ChannelReader<bool> ObserveOutboxPolls(ServiceProvider provider)
    {
        var polls = Channel.CreateUnbounded<bool>();
        var options = provider.GetRequiredService<IOptionsMonitor<PostgresMessageOutboxOptions>>()
            .Get(typeof(TransactionDbContext).FullName);
        var createConnection = options.CreateConnection;
        options.CreateConnection = services =>
        {
            var connection = createConnection(services);
            connection.StateChange += (_, args) =>
            {
                if (args.CurrentState == ConnectionState.Closed)
                {
                    polls.Writer.TryWrite(true);
                }
            };
            return connection;
        };

        return polls.Reader;
    }

    private static async Task WaitForPollStartedAfterNowAsync(ChannelReader<bool> polls)
    {
        while (polls.TryRead(out _))
        {
        }

        // The second poll starts after the first one has ended, so it starts after this call.
        await polls.ReadAsync(TestToken).AsTask().WaitAsync(s_timeout, TestToken);
        await polls.ReadAsync(TestToken).AsTask().WaitAsync(s_timeout, TestToken);
    }

    private static async Task<(int Orders, int Messages)> CountRowsAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
        return (await db.Orders.CountAsync(TestToken), await db.Set<OutboxMessage>().CountAsync(TestToken));
    }

    private static async Task WaitForEmptyOutboxAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        timeout.CancelAfter(s_timeout);

        while (await db.Set<OutboxMessage>().AnyAsync(timeout.Token))
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    public sealed class TransactionDbContext(DbContextOptions<TransactionDbContext> options) : DbContext(options)
    {
        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddPostgresOutbox();
            modelBuilder.Entity<Order>().Property(o => o.Id).ValueGeneratedNever();
        }
    }

    public sealed class Order
    {
        public int Id { get; set; }

        public int Attempt { get; set; }
    }

    public sealed record OrderPlaced(int OrderId, int Attempt);

    public sealed record PlaceOrder(int OrderId) : IEventRequest;

    public sealed class PlaceOrderHandler(TransactionDbContext db, IMessageBus bus, MessageRecorder recorder)
        : IEventRequestHandler<PlaceOrder>
    {
        public async ValueTask HandleAsync(PlaceOrder message, CancellationToken cancellationToken)
        {
            var attempt = ++recorder.Attempts;
            if (attempt == 2)
            {
                recorder.RowsBeforeRetry = (
                    await db.Orders.CountAsync(cancellationToken),
                    await db.Set<OutboxMessage>().CountAsync(cancellationToken));
            }

            db.Orders.Add(new Order { Id = message.OrderId, Attempt = attempt });
            await db.Outbox(bus).PublishAsync(new OrderPlaced(message.OrderId, attempt), cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            if (attempt == 1)
            {
                throw new RetryOrderException();
            }
        }
    }

    public sealed class OrderPlacedHandler(MessageRecorder recorder) : IEventHandler<OrderPlaced>
    {
        public ValueTask HandleAsync(OrderPlaced message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class MessageRecorder
    {
        private readonly SemaphoreSlim _received = new(0);

        public ConcurrentQueue<OrderPlaced> Messages { get; } = new();

        public int Attempts { get; set; }

        public (int Orders, int Messages) RowsBeforeRetry { get; set; }

        public void Record(OrderPlaced message)
        {
            Messages.Enqueue(message);
            _received.Release();
        }

        public async Task WaitAsync(int count, CancellationToken cancellationToken)
        {
            for (var i = 0; i < count; i++)
            {
                await _received.WaitAsync(cancellationToken).WaitAsync(s_timeout, cancellationToken);
            }
        }
    }

    private sealed class RetryOrderException : Exception;

    private sealed class SkewedTimeProvider(TimeSpan skew) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => TimeProvider.System.GetUtcNow() + skew;
    }

    private sealed class FailFirstOutboxInsertInterceptor : DbCommandInterceptor
    {
        public int Failures { get; private set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (Failures == 0 && command.CommandText.Contains("INSERT INTO outbox_messages", StringComparison.Ordinal))
            {
                Failures++;
                throw new NpgsqlException("The outbox insert failed transiently.", new TimeoutException());
            }

            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
#endif
