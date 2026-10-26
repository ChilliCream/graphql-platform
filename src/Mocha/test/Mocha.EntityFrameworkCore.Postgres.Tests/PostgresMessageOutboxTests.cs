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
        // arrange
        var (context, outbox, _) = await CreateOutboxAsync();
        await using var _ = context;
        using var __ = outbox;

        var envelope = CreateTestEnvelope();

        // act
        await outbox.PersistAsync(envelope, CancellationToken.None);

        // assert
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
        // arrange
        var (context, outbox, signal) = await CreateOutboxAsync();
        await using var _ = context;
        using var __ = outbox;

        var envelope = CreateTestEnvelope();

        // act
        await outbox.PersistAsync(envelope, CancellationToken.None);

        // assert
        Assert.True(signal.WasSet);
        Assert.Equal(1, signal.SetCallCount);
    }

    [Fact]
    public async Task PersistAsync_Should_NotSignal_When_TransactionActive()
    {
        // arrange
        var (context, outbox, signal) = await CreateOutboxAsync();
        await using var _ = context;
        using var __ = outbox;

        var envelope = CreateTestEnvelope();

        await context.Database.BeginTransactionAsync(CancellationToken.None);

        // act
        await outbox.PersistAsync(envelope, CancellationToken.None);

        // assert
        Assert.False(signal.WasSet);
        Assert.Equal(0, signal.SetCallCount);
    }

    [Fact]
    public async Task PersistAsync_Should_SerializeEnvelopeAsJson_When_Called()
    {
        // arrange
        var (context, outbox, _) = await CreateOutboxAsync();
        await using var _ = context;
        using var __ = outbox;

        var envelope = CreateTestEnvelope();

        // act
        await outbox.PersistAsync(envelope, CancellationToken.None);

        // assert
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
    public void Dispose_Should_NotThrow_When_Called()
    {
        // arrange
        var options = new DbContextOptionsBuilder<TestDbContext>().UseTestNpgsql("Host=localhost;Database=test").Options;
        using var context = new TestDbContext(options);
        var queries = PostgresMessageOutboxQueries.From(new OutboxTableInfo());
        var signal = new StubOutboxSignal();
        var outbox = new PostgresMessageOutbox(
            context,
            signal,
            queries.InsertEnvelope,
            requireTransaction: false);

        // act
        var ex = Record.Exception(outbox.Dispose);

        // assert
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
        var outbox = new PostgresMessageOutbox(
            context,
            signal,
            queries.InsertEnvelope,
            requireTransaction: false);
        return (context, outbox, signal);
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
