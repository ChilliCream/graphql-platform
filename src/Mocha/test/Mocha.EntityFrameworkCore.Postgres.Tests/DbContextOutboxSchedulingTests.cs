#if NET9_0_OR_GREATER
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Transactions;
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
    [InlineData(false)]
    [InlineData(true)]
    public async Task DispatchAsync_Should_PersistWithBusinessChanges_When_OptionsSpecifyScheduledTime(bool send)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
        var scheduledTime = new DateTimeOffset(2050, 1, 1, 0, 0, 0, TimeSpan.Zero);
        db.Orders.Add(new Order { Id = 1 });

        // act
        if (send)
        {
            await db.Outbox(bus).SendAsync(
                new ScheduledCommand("scheduled"), new SendOptions { ScheduledTime = scheduledTime }, TestToken);
        }
        else
        {
            await db.Outbox(bus).PublishAsync(
                new ScheduledEvent("scheduled"), new PublishOptions { ScheduledTime = scheduledTime }, TestToken);
        }

        var beforeSave = await ReadCountsAsync(connectionString);
        await db.SaveChangesAsync(TestToken);

        await using var verification = provider.CreateAsyncScope();
        var saved = await verification.ServiceProvider.GetRequiredService<SchedulingDbContext>()
            .Set<OutboxMessage>().AsNoTracking().SingleAsync(TestToken);
        using (saved.Envelope)
        {
            var envelope = MessageEnvelopeReader.Parse(Encoding.UTF8.GetBytes(saved.Envelope.RootElement.GetRawText()));

            // assert
            Assert.Equal("orders=0,outbox=0", beforeSave);
            Assert.Equal("orders=1,outbox=1", await ReadCountsAsync(connectionString));
            Assert.Equal(scheduledTime, envelope.ScheduledTime);
            Assert.Equal(
                send ? MessageKind.Send : MessageKind.Publish,
                envelope.Headers!.Get(MessageHeaders.MessageKind));
            Assert.Equal("scheduled", JsonSerializer.Deserialize<ScheduledEvent>(envelope.Body.Span)!.Payload);
        }
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
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            var db = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
            await db.Outbox(bus).PublishAsync(
                new ScheduledEvent("ambient"), new PublishOptions { ScheduledTime = DateTimeOffset.UtcNow }, TestToken);
            await db.SaveChangesAsync(TestToken);
            signal.Events.Enqueue("before-complete");
            transaction.Complete();
        }

        // assert
        Assert.Equal(["before-complete", "notify(rows=1)"], signal.Events.Distinct().ToArray());
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverAtScheduledTime_When_InMemorySchedulingIsUsed()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
        var recorder = provider.GetRequiredService<Recorder>();
        var scheduledTime = DateTimeOffset.UtcNow.AddSeconds(2);

        // act
        await db.Outbox(bus).PublishAsync(
            new ScheduledEvent("delivered"),
            new PublishOptions { ScheduledTime = scheduledTime },
            TestToken);
        await db.SaveChangesAsync(TestToken);
        var afterSave = await ReadCountsAsync(connectionString);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var delivered = await recorder.Delivered.Task.WaitAsync(TimeSpan.FromSeconds(15), TestToken);

        // assert
        Assert.Equal("orders=0,outbox=1", afterSave);
        Assert.Equal("delivered", delivered.Payload);
        Assert.InRange(delivered.ReceivedAt, scheduledTime, DateTimeOffset.MaxValue);
    }

    private static async Task<ServiceProvider> CreateProviderAsync(
        string connectionString,
        IOutboxSignal? signal = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Recorder>();
        services.AddKeyedSingleton<IOutboxSignal>(typeof(SchedulingDbContext), signal ?? new StubOutboxSignal());

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<SchedulingDbContext>(ef => ef.UsePostgresOutbox());
        services.AddDbContext<SchedulingDbContext>(options => options.UseTestNpgsql(connectionString));

        builder.AddEventHandler<ScheduledEventHandler>();
        builder.AddRequestHandler<ScheduledCommandHandler>();
        builder.AddInMemory();

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SchedulingDbContext>().Database.EnsureCreatedAsync(TestToken);

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

    public sealed record Delivery(string Payload, DateTimeOffset ReceivedAt);

    public sealed class Recorder
    {
        public TaskCompletionSource<Delivery> Delivered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class ScheduledEventHandler(Recorder recorder) : IEventHandler<ScheduledEvent>
    {
        public ValueTask HandleAsync(ScheduledEvent message, CancellationToken cancellationToken)
        {
            recorder.Delivered.TrySetResult(new Delivery(message.Payload, DateTimeOffset.UtcNow));
            return default;
        }
    }

    public sealed class ScheduledCommandHandler(Recorder recorder) : IEventRequestHandler<ScheduledCommand>
    {
        public ValueTask HandleAsync(ScheduledCommand message, CancellationToken cancellationToken)
        {
            recorder.Delivered.TrySetResult(new Delivery(message.Payload, DateTimeOffset.UtcNow));
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
#endif
