using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Mocha.EntityFrameworkCore.SqlServer.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Outbox;

namespace Mocha.EntityFrameworkCore.SqlServer.Tests;

public sealed class SqlServerMessageOutboxTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public SqlServerMessageOutboxTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
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
        var connection = (SqlConnection)context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(CancellationToken.None);
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM [dbo].[outbox_messages]";
        var count = (int)(await cmd.ExecuteScalarAsync(CancellationToken.None))!;

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
        var connection = (SqlConnection)context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(CancellationToken.None);
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT TOP(1) [envelope] FROM [dbo].[outbox_messages]";
        var json = (string)(await cmd.ExecuteScalarAsync(CancellationToken.None))!;

        Assert.Contains("\"messageId\"", json);
        Assert.Contains("test-event", json);
        Assert.Contains("memory://test/queue", json);
    }

    [Fact]
    public void Dispose_Should_NotThrow_When_Called()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlServer("Server=localhost;Database=test;TrustServerCertificate=True").Options;
        using var context = new TestDbContext(options);
        var queries = SqlServerMessageOutboxQueries.From(new OutboxTableInfo());
        var signal = new StubOutboxSignal();
        var outbox = new SqlServerMessageOutbox(context, signal, queries.InsertEnvelope);

        // Act & Assert
        var ex = Record.Exception(outbox.Dispose);
        Assert.Null(ex);
    }

    private async Task<(
        TestDbContext Context,
        SqlServerMessageOutbox Outbox,
        StubOutboxSignal Signal)> CreateOutboxAsync()
    {
        var connectionString = await _fixture.CreateDatabaseAsync();
        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlServer(connectionString).Options;
        var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var queries = SqlServerMessageOutboxQueries.From(new OutboxTableInfo());
        var signal = new StubOutboxSignal();
        var outbox = new SqlServerMessageOutbox(context, signal, queries.InsertEnvelope);
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
            Body = "{\"value\":42}"u8.ToArray()
        };
    }
}
