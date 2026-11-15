using System.Collections.Concurrent;
using System.Data;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
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
        var provider = await CreateProviderAsync(connectionString, TimeSpan.FromMinutes(1));
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

        provider.GetRequiredService<IOutboxSignal>().Set();
        await polls.ReadAsync(TestToken).AsTask().WaitAsync(s_timeout, TestToken);
        var deliveredBeforeCommit = recorder.Messages.ToArray();
        await transaction.CommitAsync(TestToken);
        await recorder.WaitAsync(2, TestToken);
        await WaitForEmptyOutboxAsync(provider);
        var orderIds = await db.Orders.OrderBy(o => o.Id).Select(o => o.Id).ToArrayAsync(TestToken);

        // assert
        Assert.Equal(((0, 0), (0, 0)), (afterFirstSave, afterSecondSave));
        Assert.Empty(deliveredBeforeCommit);
        Assert.Equal([1, 2], orderIds);
        Assert.Equal([new OrderPlaced(1, 1), new OrderPlaced(2, 1)], recorder.Messages.OrderBy(m => m.OrderId));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_RollBackFailedAttempt_When_ConsumerSavesBeforeRetrying()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var provider = await CreateProviderAsync(connectionString, TimeSpan.FromMinutes(1));
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
        Assert.Equal((1, 2), (order.Id, order.Attempt));
        Assert.Equal([new OrderPlaced(1, 2)], recorder.Messages);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_DeliverAfterCommit_When_TransactionIsCommittedOutsideEntityFramework()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var provider = await CreateProviderAsync(connectionString, TimeSpan.FromMilliseconds(100));
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

    private static async Task<ServiceProvider> CreateProviderAsync(string connectionString, TimeSpan queryDelay)
    {
        await using (var db = new TransactionDbContext(new DbContextOptionsBuilder<TransactionDbContext>()
            .UseTestNpgsql(connectionString).Options))
        {
            await db.Database.EnsureCreatedAsync(TestToken);
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<MessageRecorder>();
        services.AddDbContext<TransactionDbContext>(options => options.UseTestNpgsql(connectionString));
        services.AddMessageBus()
            .AddEntityFramework<TransactionDbContext>(ef =>
            {
                ef.UsePostgresOutbox(options => options.QueryDelay = queryDelay);
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
}
