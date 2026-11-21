#if NET9_0_OR_GREATER
using System.Collections.Concurrent;
using System.Data;
using System.Threading.Channels;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Mocha.Transport.InMemory;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresDbContextOutboxEnlistmentTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private const string AtomicSaveMessage =
        "The outbox for DbContext "
        + "'Mocha.EntityFrameworkCore.Postgres.Tests.PostgresDbContextOutboxEnlistmentTests+EnlistmentDbContext' "
        + "requires an atomic save. "
        + "Enable automatic transactions or begin an explicit or ambient transaction before saving.";

    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SaveChangesAsync_Should_Throw_When_ConnectionDoesNotJoinAmbientTransaction()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var provider = await CreateProviderAsync(connectionString, enlist: false);
        var polls = ObserveOutboxPolls(provider);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var recorder = provider.GetRequiredService<MessageRecorder>();
        InvalidOperationException exception;

        // act
        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<EnlistmentDbContext>();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            db.Orders.Add(new Order { Id = 1 });
            await db.Outbox(bus).PublishAsync(new OrderPlaced(1), TestToken);
            db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
            exception = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(TestToken));
        }

        await WaitForPollStartedAfterNowAsync(polls);
        var rows = await CountRowsAsync(provider);

        // assert
        Assert.Equal(AtomicSaveMessage, exception.Message);
        Assert.Equal((0, 0), rows);
        Assert.Empty(recorder.Messages);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_DeliverAfterCommit_When_ContextIsEnlistedInCommittableTransaction()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var provider = await CreateProviderAsync(connectionString, enlist: true);
        var polls = ObserveOutboxPolls(provider);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var recorder = provider.GetRequiredService<MessageRecorder>();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EnlistmentDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
        using var transaction = new CommittableTransaction();
        await db.Database.OpenConnectionAsync(TestToken);
        db.Database.EnlistTransaction(transaction);

        // act
        db.Orders.Add(new Order { Id = 1 });
        await db.Outbox(bus).PublishAsync(new OrderPlaced(1), TestToken);
        await db.SaveChangesAsync(TestToken);
        var beforeCommit = await CountRowsAsync(provider);
        await WaitForPollStartedAfterNowAsync(polls);
        var deliveredBeforeCommit = recorder.Messages.ToArray();
        transaction.Commit();
        await recorder.WaitAsync(1, TestToken);
        await WaitForEmptyOutboxAsync(provider);
        var afterCommit = await CountRowsAsync(provider);

        // assert
        Assert.Equal((0, 0), beforeCommit);
        Assert.Empty(deliveredBeforeCommit);
        Assert.Equal((1, 0), afterCommit);
        Assert.Equal([new OrderPlaced(1)], recorder.Messages);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_DiscardSave_When_EnlistedCommittableTransactionIsRolledBack()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var provider = await CreateProviderAsync(connectionString, enlist: true);
        var polls = ObserveOutboxPolls(provider);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var recorder = provider.GetRequiredService<MessageRecorder>();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EnlistmentDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
        using var transaction = new CommittableTransaction();
        await db.Database.OpenConnectionAsync(TestToken);
        db.Database.EnlistTransaction(transaction);

        // act
        db.Orders.Add(new Order { Id = 1 });
        await db.Outbox(bus).PublishAsync(new OrderPlaced(1), TestToken);
        await db.SaveChangesAsync(TestToken);
        transaction.Rollback();
        await WaitForPollStartedAfterNowAsync(polls);
        var afterRollback = await CountRowsAsync(provider);

        // assert
        Assert.Equal((0, 0), afterRollback);
        Assert.Empty(recorder.Messages);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_DeliverAfterCompletion_When_PublishedBeforeConnectionOpensInTransactionScope()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var provider = await CreateProviderAsync(connectionString, enlist: true);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var recorder = provider.GetRequiredService<MessageRecorder>();
        ConnectionState stateAtPublish;

        // act
        using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<EnlistmentDbContext>();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
            stateAtPublish = db.Database.GetDbConnection().State;
            db.Orders.Add(new Order { Id = 1 });
            await db.Outbox(bus).PublishAsync(new OrderPlaced(1), TestToken);
            await db.SaveChangesAsync(TestToken);
            transaction.Complete();
        }

        await recorder.WaitAsync(1, TestToken);
        await WaitForEmptyOutboxAsync(provider);
        var rows = await CountRowsAsync(provider);

        // assert
        Assert.Equal(ConnectionState.Closed, stateAtPublish);
        Assert.Equal([new OrderPlaced(1)], recorder.Messages);
        Assert.Equal((1, 0), rows);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_DiscardSave_When_PublishedBeforeConnectionOpensInIncompleteTransactionScope()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var provider = await CreateProviderAsync(connectionString, enlist: true);
        var polls = ObserveOutboxPolls(provider);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var recorder = provider.GetRequiredService<MessageRecorder>();
        ConnectionState stateAtPublish;

        // act
        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<EnlistmentDbContext>();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
            stateAtPublish = db.Database.GetDbConnection().State;
            db.Orders.Add(new Order { Id = 1 });
            await db.Outbox(bus).PublishAsync(new OrderPlaced(1), TestToken);
            await db.SaveChangesAsync(TestToken);
        }

        await WaitForPollStartedAfterNowAsync(polls);
        var rows = await CountRowsAsync(provider);

        // assert
        Assert.Equal(ConnectionState.Closed, stateAtPublish);
        Assert.Equal((0, 0), rows);
        Assert.Empty(recorder.Messages);
    }

    private static async Task<ServiceProvider> CreateProviderAsync(string connectionString, bool enlist)
    {
        await using (var db = new EnlistmentDbContext(new DbContextOptionsBuilder<EnlistmentDbContext>()
            .UseTestNpgsql(connectionString).Options))
        {
            await db.Database.EnsureCreatedAsync(TestToken);
        }

        var contextConnectionString = enlist
            ? connectionString
            : new NpgsqlConnectionStringBuilder(connectionString) { Enlist = false }.ConnectionString;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<MessageRecorder>();
        services.AddDbContext<EnlistmentDbContext>(options => options.UseTestNpgsql(contextConnectionString));
        services.AddMessageBus()
            .AddEntityFramework<EnlistmentDbContext>(
                ef => ef.UsePostgresOutbox(options => options.PollingInterval = TimeSpan.FromMilliseconds(100)))
            .AddEventHandler<OrderPlacedHandler>()
            .AddInMemory();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static ChannelReader<bool> ObserveOutboxPolls(ServiceProvider provider)
    {
        var polls = Channel.CreateUnbounded<bool>();
        var options = provider.GetRequiredService<IOptionsMonitor<PostgresMessageOutboxOptions>>()
            .Get(typeof(EnlistmentDbContext).FullName);
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
        var db = scope.ServiceProvider.GetRequiredService<EnlistmentDbContext>();
        return (await db.Orders.CountAsync(TestToken), await db.Set<OutboxMessage>().CountAsync(TestToken));
    }

    private static async Task WaitForEmptyOutboxAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EnlistmentDbContext>();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        timeout.CancelAfter(s_timeout);

        while (await db.Set<OutboxMessage>().AnyAsync(timeout.Token))
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    public sealed class EnlistmentDbContext(DbContextOptions<EnlistmentDbContext> options) : DbContext(options)
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
    }

    public sealed record OrderPlaced(int OrderId);

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
}
#endif
