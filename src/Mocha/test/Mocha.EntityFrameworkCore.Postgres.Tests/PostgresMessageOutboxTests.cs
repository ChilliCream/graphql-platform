using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Outbox;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresMessageOutboxTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public PostgresMessageOutboxTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PersistAsync_Should_UseNamedOptions_When_TransactionRequirementIsConfigured()
    {
        // arrange
        var connectionString = await _fixture.CreateDatabaseAsync();
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options => options.UseTestNpgsql(connectionString));
        services.AddSingleton<IOutboxSignal, ResilientOutboxSignal>();
        services.AddMessageBus().AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        services.PostConfigure<PostgresOutboxOptions>(
            typeof(TestDbContext).FullName,
            options => options.RequireTransaction = false);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var outbox = scope.ServiceProvider.GetRequiredService<IMessageOutbox>();
        var envelope = CreateTestEnvelope();
        envelope.Headers!.Set(MessageHeaders.MessageKind, MessageKind.Publish);

        // act
        await outbox.PersistAsync(envelope, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, await context.Set<OutboxMessage>().CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PersistAsync_Should_InsertRow_When_Called()
    {
        // Arrange
        var (context, outbox, _) = await CreateOutboxAsync();
        await using var _ = context;
        using var __ = outbox;

        var envelope = CreateTestEnvelope();

        // Act
        await outbox.PersistAsync(envelope, CancellationToken.None);

        // Assert
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(CancellationToken.None);
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM \"outbox_messages\"";
        var count = (long)(await cmd.ExecuteScalarAsync(CancellationToken.None))!;

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task PersistAsync_Should_SignalOutbox_When_NoTransaction()
    {
        // Arrange
        var (context, outbox, signal) = await CreateOutboxAsync();
        await using var _ = context;
        using var __ = outbox;

        var envelope = CreateTestEnvelope();

        // Act
        await outbox.PersistAsync(envelope, CancellationToken.None);

        // Assert
        Assert.True(signal.WasSet);
        Assert.Equal(1, signal.SetCallCount);
    }

    [Fact]
    public async Task PersistAsync_Should_NotSignal_When_TransactionActive()
    {
        // Arrange
        var (context, outbox, signal) = await CreateOutboxAsync();
        await using var _ = context;
        using var __ = outbox;

        var envelope = CreateTestEnvelope();

        await context.Database.BeginTransactionAsync(CancellationToken.None);

        // Act
        await outbox.PersistAsync(envelope, CancellationToken.None);

        // Assert
        Assert.False(signal.WasSet);
        Assert.Equal(0, signal.SetCallCount);
    }

    [Fact]
    public async Task PersistAsync_Should_SerializeEnvelopeAsJson_When_Called()
    {
        // Arrange
        var (context, outbox, _) = await CreateOutboxAsync();
        await using var _ = context;
        using var __ = outbox;

        var envelope = CreateTestEnvelope();

        // Act
        await outbox.PersistAsync(envelope, CancellationToken.None);

        // Assert
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(CancellationToken.None);
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT \"envelope\"::text FROM \"outbox_messages\" LIMIT 1";
        var json = (string)(await cmd.ExecuteScalarAsync(CancellationToken.None))!;

        Assert.Contains("\"messageId\"", json);
        Assert.Contains("test-event", json);
        Assert.Contains("memory://test/queue", json);
    }

    [Fact]
    public async Task PersistAsync_Should_UseDatabaseTransactionId_When_DatabaseTransactionIsActive()
    {
        // arrange
        var (context, outbox, _) = await CreateOutboxAsync();
        await using var _ = context;
        using var __ = outbox;
        Guid expected;

        // act
        await using (var transaction = await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken))
        {
            expected = transaction.TransactionId;
            await outbox.PersistAsync(CreateTestEnvelope(), TestContext.Current.CancellationToken);
            await outbox.PersistAsync(CreateTestEnvelope(), TestContext.Current.CancellationToken);
            await transaction.CommitAsync(TestContext.Current.CancellationToken);
        }

        // assert
        var rows = await ReadRowsAsync(context);
        Assert.Equal([expected, expected], rows.Select(r => r.TransactionId));
    }

    [Fact]
    public async Task PersistAsync_Should_ShareTransactionId_When_AmbientTransactionIsActive()
    {
        // arrange
        var (context, outbox, _) = await CreateOutboxAsync();
        await using var _ = context;
        using var __ = outbox;

        // act
        using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await outbox.PersistAsync(CreateTestEnvelope(), TestContext.Current.CancellationToken);
            await outbox.PersistAsync(CreateTestEnvelope(), TestContext.Current.CancellationToken);
            scope.Complete();
        }

        // assert
        var rows = await ReadRowsAsync(context);
        Assert.Equal([rows[0].Id, rows[0].Id], rows.Select(r => r.TransactionId));
    }

    [Fact]
    public async Task PersistAsync_Should_UseNewTransactionId_When_AmbientTransactionChanges()
    {
        // arrange
        var (context, outbox, _) = await CreateOutboxAsync();
        await using var _ = context;
        using var __ = outbox;

        // act
        for (var i = 0; i < 2; i++)
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            await outbox.PersistAsync(CreateTestEnvelope(), TestContext.Current.CancellationToken);
            scope.Complete();
        }

        // assert
        var rows = await ReadRowsAsync(context);
        Assert.Equal([rows[0].Id, rows[1].Id], rows.Select(r => r.TransactionId));
    }

    [Fact]
    public async Task PersistAsync_Should_UseMessageIdAsTransactionId_When_NoTransactionIsActive()
    {
        // arrange
        var (context, outbox, _) = await CreateOutboxAsync();
        await using var _ = context;
        using var __ = outbox;

        // act
        await outbox.PersistAsync(CreateTestEnvelope(), TestContext.Current.CancellationToken);
        await outbox.PersistAsync(CreateTestEnvelope(), TestContext.Current.CancellationToken);

        // assert
        var rows = await ReadRowsAsync(context);
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Equal(r.Id, r.TransactionId));
    }

    [Fact]
    public void Dispose_Should_NotThrow_When_Called()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>().UseTestNpgsql("Host=localhost;Database=test").Options;
        using var context = new TestDbContext(options);
        var queries = PostgresMessageOutboxQueries.From(new OutboxTableInfo());
        var signal = new StubOutboxSignal();
        var outbox = new PostgresMessageOutbox(context, signal, queries.InsertEnvelope, requireTransaction: false);

        // Act & Assert
        var ex = Record.Exception(outbox.Dispose);
        Assert.Null(ex);
    }

    private async Task<(
        TestDbContext Context,
        PostgresMessageOutbox Outbox,
        StubOutboxSignal Signal)> CreateOutboxAsync()
    {
        var connectionString = await _fixture.CreateDatabaseAsync();
        var options = new DbContextOptionsBuilder<TestDbContext>().UseTestNpgsql(connectionString).Options;
        var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var queries = PostgresMessageOutboxQueries.From(new OutboxTableInfo());
        var signal = new StubOutboxSignal();
        var outbox = new PostgresMessageOutbox(context, signal, queries.InsertEnvelope, requireTransaction: false);
        return (context, outbox, signal);
    }

    private static async Task<List<(Guid Id, Guid? TransactionId)>> ReadRowsAsync(TestDbContext context)
    {
        var rows = await context.Set<OutboxMessage>()
            .AsNoTracking()
            .OrderBy(m => m.Id)
            .Select(m => new { m.Id, m.TransactionId })
            .ToListAsync(TestContext.Current.CancellationToken);

        return rows.Select(r => (r.Id, r.TransactionId)).ToList();
    }

    private static MessageEnvelope CreateTestEnvelope()
    {
        return new MessageEnvelope
        {
            MessageId = Guid.NewGuid().ToString(),
            MessageType = "urn:message:test-event",
            DestinationAddress = "memory://test/queue",
            SentAt = DateTimeOffset.UtcNow,
            Headers = new Headers(),
            Body = "{\"value\":42}"u8.ToArray()
        };
    }
}
