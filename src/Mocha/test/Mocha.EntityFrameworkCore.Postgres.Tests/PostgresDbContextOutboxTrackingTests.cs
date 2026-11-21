#if NET9_0_OR_GREATER
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Mocha.Transport.InMemory;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresDbContextOutboxTrackingTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private const string AtomicSaveMessage =
        "The outbox for DbContext "
        + "'Mocha.EntityFrameworkCore.Postgres.Tests.PostgresDbContextOutboxTrackingTests+TrackingDbContext' "
        + "requires an atomic save. "
        + "Enable automatic transactions or begin an explicit or ambient transaction before saving.";

    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SaveChangesAsync_Should_WriteOrderAndMessage_When_UnchangedEntriesAreAddedAgainAfterRollback()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<TrackingDbContext>();
        Guid messageId;
        await using (var transaction = await db.Database.BeginTransactionAsync(TestToken))
        {
            db.Orders.Add(new Order { Id = 1 });
            await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
            messageId = db.ChangeTracker.Entries<OutboxMessage>().Single().Entity.Id;
            await db.SaveChangesAsync(TestToken);
            await transaction.RollbackAsync(TestToken);
        }

        // act
        foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State == EntityState.Unchanged).ToList())
        {
            entry.State = EntityState.Added;
        }

        await using (var transaction = await db.Database.BeginTransactionAsync(TestToken))
        {
            await db.SaveChangesAsync(TestToken);
            await transaction.CommitAsync(TestToken);
        }

        // assert
        Assert.Equal([1], await ReadOrderIdsAsync(connectionString));
        Assert.Equal([messageId], await ReadOutboxIdsAsync(connectionString));
    }

    [Fact]
    public async Task SaveChangesAsync_Should_WriteMessage_When_KeptEntryIsAddedAgainAfterRollback()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<TrackingDbContext>();
        var order = db.Orders.Add(new Order { Id = 1 });
        await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        var message = db.ChangeTracker.Entries<OutboxMessage>().Single();
        await using (var transaction = await db.Database.BeginTransactionAsync(TestToken))
        {
            await db.SaveChangesAsync(TestToken);
            await transaction.RollbackAsync(TestToken);
        }

        // act
        order.State = EntityState.Added;
        message.State = EntityState.Added;
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal([1], await ReadOrderIdsAsync(connectionString));
        Assert.Equal([message.Entity.Id], await ReadOutboxIdsAsync(connectionString));
    }

    [Fact]
    public async Task SaveChangesAsync_Should_InsertMessageOnce_When_StagedMessageIsDetachedAndAddedAgain()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<TrackingDbContext>();
        await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        var message = db.ChangeTracker.Entries<OutboxMessage>().Single();
        message.State = EntityState.Detached;
        db.Add(message.Entity);

        // act
        await db.SaveChangesAsync(TestToken);
        db.Orders.Add(new Order { Id = 2 });
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal([message.Entity.Id], await ReadOutboxIdsAsync(connectionString));
        Assert.Equal([2], await ReadOrderIdsAsync(connectionString));
    }

    [Fact]
    public async Task SaveChangesAsync_Should_KeepMessageTrackedAsUnchanged_When_SaveSucceeds()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<TrackingDbContext>();
        await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 7 }, TestToken);

        // act
        await db.SaveChangesAsync(TestToken);

        // assert
        var message = Assert.Single(db.ChangeTracker.Entries<OutboxMessage>());
        Assert.Equal(EntityState.Unchanged, message.State);
        Assert.Equal(
            7,
            message.Entity.Envelope.RootElement.GetProperty("body").Deserialize<OrderPlaced>()!.OrderId);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_WriteNoMessage_When_PooledContextWasReturnedWithStagedMessage()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, pooled: true);
        TrackingDbContext leased;
        await using (var scope = provider.CreateAsyncScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            leased = scope.ServiceProvider.GetRequiredService<TrackingDbContext>();
            await leased.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        }

        await using var next = provider.CreateAsyncScope();
        var db = next.ServiceProvider.GetRequiredService<TrackingDbContext>();

        // act
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Same(leased, db);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Empty(await ReadOutboxIdsAsync(connectionString));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveChanges_Should_Throw_When_TransactionOfStagedMessageEndedBeforeSave(bool asynchronous)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<TrackingDbContext>();
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
        await using (var transaction = await db.Database.BeginTransactionAsync(TestToken))
        {
            await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
            await transaction.CommitAsync(TestToken);
        }

        db.Orders.Add(new Order { Id = 1 });

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            if (asynchronous)
            {
                await db.SaveChangesAsync(TestToken);
            }
            else
            {
                db.SaveChanges();
            }
        });

        // assert
        Assert.Equal(AtomicSaveMessage, exception.Message);
        Assert.Equal([EntityState.Added], db.ChangeTracker.Entries<OutboxMessage>().Select(entry => entry.State));
        Assert.Empty(await ReadOrderIdsAsync(connectionString));
        Assert.Empty(await ReadOutboxIdsAsync(connectionString));
    }

    [Fact]
    public async Task UsePostgresOutbox_Should_RegisterAtomicSaveInterceptorOnce_When_CalledTwice()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();

        // act
        await using var provider = await CreateProviderAsync(connectionString, configureOutboxTwice: true);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TrackingDbContext>();

        // assert
        Assert.Single(db.GetService<IEnumerable<IInterceptor>>().OfType<OutboxAtomicSaveInterceptor>());
    }

    private static async Task<ServiceProvider> CreateProviderAsync(
        string connectionString,
        bool pooled = false,
        bool configureOutboxTwice = false)
    {
        await using (var db = new TrackingDbContext(new DbContextOptionsBuilder<TrackingDbContext>()
            .UseTestNpgsql(connectionString).Options))
        {
            await db.Database.EnsureCreatedAsync(TestToken);
        }

        var services = new ServiceCollection();
        services.AddLogging();

        if (pooled)
        {
            services.AddDbContextPool<TrackingDbContext>(options => options.UseTestNpgsql(connectionString));
        }
        else
        {
            services.AddDbContext<TrackingDbContext>(options => options.UseTestNpgsql(connectionString));
        }

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TrackingDbContext>(ef => ef.UsePostgresOutbox());

        if (configureOutboxTwice)
        {
            builder.AddEntityFramework<TrackingDbContext>(ef => ef.UsePostgresOutbox());
        }

        builder.AddEventHandler<OrderPlacedHandler>();
        builder.AddInMemory();

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestToken);

        if (pooled)
        {
            // Complete table metadata discovery before testing context leases.
            provider.GetRequiredService<IOptionsMonitor<PostgresMessageOutboxOptions>>()
                .Get(typeof(TrackingDbContext).FullName);
        }

        return provider;
    }

    private static Task<List<int>> ReadOrderIdsAsync(string connectionString)
        => ReadColumnAsync<int>(connectionString, """SELECT "Id" FROM orders ORDER BY "Id" """);

    private static Task<List<Guid>> ReadOutboxIdsAsync(string connectionString)
        => ReadColumnAsync<Guid>(connectionString, "SELECT id FROM outbox_messages ORDER BY id");

    private static async Task<List<T>> ReadColumnAsync<T>(string connectionString, string sql)
    {
        var settings = new NpgsqlConnectionStringBuilder(connectionString) { Enlist = false };
        await using var connection = new NpgsqlConnection(settings.ConnectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(TestToken);
        var values = new List<T>();

        while (await reader.ReadAsync(TestToken))
        {
            values.Add(reader.GetFieldValue<T>(0));
        }

        return values;
    }

    public sealed class TrackingDbContext(DbContextOptions<TrackingDbContext> options) : DbContext(options)
    {
        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddPostgresOutbox();
            modelBuilder.Entity<Order>().ToTable("orders").Property(o => o.Id).ValueGeneratedNever();
        }
    }

    public sealed class Order
    {
        public int Id { get; set; }
    }

    public sealed class OrderPlaced
    {
        public required int OrderId { get; set; }
    }

    public sealed class OrderPlacedHandler : IEventHandler<OrderPlaced>
    {
        public ValueTask HandleAsync(OrderPlaced message, CancellationToken cancellationToken) => default;
    }
}
#endif
