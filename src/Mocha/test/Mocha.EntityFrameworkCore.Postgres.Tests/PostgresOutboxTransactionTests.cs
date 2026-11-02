using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Transport.InMemory;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxTransactionTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task PublishAsync_Should_Throw_When_TransactionIsRequiredAndNoneIsActive()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await bus.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken));

        // assert
        Assert.Equal(
            "The outbox for DbContext 'Mocha.EntityFrameworkCore.Postgres.Tests.PostgresOutboxTransactionTests+TransactionDbContext' "
            + "requires a transaction to publish or send a message. "
            + "Begin a transaction on that DbContext, use UseTransaction() for consumers, "
            + "or set RequireTransaction to false in UsePostgresOutbox().",
            exception.Message);
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
    }

    [Fact]
    public async Task PublishAsync_Should_InsertRowImmediately_When_TransactionIsNotRequired()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var signal = new StubOutboxSignal();
        await using var provider = await CreateProviderAsync(connectionString, signal, requireTransaction: false);
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);

        // assert
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
        Assert.True(signal.WasSet);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublishAsync_Should_InsertRowWithTransaction_When_DatabaseTransactionIsActive(
        bool requireTransaction)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            requireTransaction: requireTransaction);
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();

        // act
        await using var transaction = await db.Database.BeginTransactionAsync(TestToken);
        await bus.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        var rowsBeforeCommit = await CountOutboxRowsAsync(connectionString);
        await transaction.CommitAsync(TestToken);
        var rowsAfterCommit = await CountOutboxRowsAsync(connectionString);

        // assert
        Assert.Equal((0L, 1L), (rowsBeforeCommit, rowsAfterCommit));
    }

    [Fact]
    public async Task PublishAsync_Should_InsertRowWithTransaction_When_AmbientTransactionIsActive()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        long rowsBeforeComplete;

        // act
        using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await using var scope = provider.CreateAsyncScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
            rowsBeforeComplete = await CountOutboxRowsAsync(connectionString);
            transaction.Complete();
        }

        var rowsAfterComplete = await CountOutboxRowsAsync(connectionString);

        // assert
        Assert.Equal((0L, 1L), (rowsBeforeComplete, rowsAfterComplete));
    }

    [Theory]
    [InlineData(MessageKind.Publish)]
    [InlineData(MessageKind.Send)]
    public async Task PersistAsync_Should_Throw_When_PublishOrSendHasNoTransaction(string messageKind)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IMessageOutbox>();
        var envelope = CreateEnvelope(messageKind);

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await outbox.PersistAsync(envelope, TestToken));

        // assert
        Assert.Equal(
            "The outbox for DbContext 'Mocha.EntityFrameworkCore.Postgres.Tests.PostgresOutboxTransactionTests+TransactionDbContext' "
            + "requires a transaction to publish or send a message. "
            + "Begin a transaction on that DbContext, use UseTransaction() for consumers, "
            + "or set RequireTransaction to false in UsePostgresOutbox().",
            exception.Message);
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
    }

    [Theory]
    [InlineData(MessageKind.Reply)]
    [InlineData(MessageKind.Fault)]
    public async Task PersistAsync_Should_InsertRow_When_ReplyOrFaultHasNoTransaction(string messageKind)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var signal = new StubOutboxSignal();
        await using var provider = await CreateProviderAsync(connectionString, signal);
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IMessageOutbox>();
        var envelope = CreateEnvelope(messageKind);

        // act
        await outbox.PersistAsync(envelope, TestToken);

        // assert
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
        Assert.True(signal.WasSet);
    }

    private static MessageEnvelope CreateEnvelope(string messageKind)
    {
        var headers = new Headers();
        headers.Set(MessageHeaders.MessageKind, messageKind);
        return new MessageEnvelope
        {
            MessageId = Guid.NewGuid().ToString(),
            MessageType = "urn:message:order-placed",
            DestinationAddress = "memory://test/queue",
            SentAt = DateTimeOffset.UtcNow,
            Headers = headers,
            Body = "{\"orderId\":1}"u8.ToArray()
        };
    }

    private static async Task EnsureCreatedAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
        await db.Database.EnsureCreatedAsync(TestToken);
    }

    private static async Task<long> CountOutboxRowsAsync(string connectionString)
    {
        var settings = new NpgsqlConnectionStringBuilder(connectionString) { Enlist = false };
        await using var connection = new NpgsqlConnection(settings.ConnectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM outbox_messages";
        return (long)(await command.ExecuteScalarAsync(TestToken))!;
    }

    private static async Task<ServiceProvider> CreateProviderAsync(
        string connectionString,
        IOutboxSignal signal,
        bool requireTransaction = true)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(signal);
        services.AddDbContext<TransactionDbContext>(o => o.UseTestNpgsql(connectionString));

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TransactionDbContext>(ef =>
        {
            if (requireTransaction)
            {
                ef.UsePostgresOutbox();
            }
            else
            {
                ef.UsePostgresOutbox(o => o.RequireTransaction = false);
            }
        });
        builder.AddEventHandler<OrderPlacedHandler>();
        builder.AddInMemory();

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestToken);
        return provider;
    }

    public sealed class TransactionDbContext(DbContextOptions<TransactionDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddPostgresOutbox();
        }
    }

    public sealed class OrderPlaced
    {
        public required int OrderId { get; init; }
    }

    public sealed class OrderPlacedHandler : IEventHandler<OrderPlaced>
    {
        public ValueTask HandleAsync(OrderPlaced message, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}
