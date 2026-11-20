using Microsoft.EntityFrameworkCore;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Outbox;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxQueriesTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly PostgresMessageOutboxQueries s_queries =
        PostgresMessageOutboxQueries.From(new OutboxTableInfo());

    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ProcessEvent_Should_ReturnRowsInIdOrder_When_TransactionHasSeveralRows()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var transactionId = Guid.NewGuid();
        var expected = await InsertAsync(connectionString, transactionId, 3);
        await using var connection = await OpenAsync(connectionString);
        await using var transaction = await connection.BeginTransactionAsync(TestToken);

        // act
        var claimed = await ClaimAsync(connection, transaction);

        // assert
        Assert.Equal(expected, claimed);
    }

    [Fact]
    public async Task ProcessEvent_Should_ReturnBatchSizeRows_When_TransactionIsLarger()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var inserted = await InsertAsync(connectionString, Guid.NewGuid(), PostgresMessageOutboxQueries.BatchSize + 5);
        await using var connection = await OpenAsync(connectionString);
        await using var transaction = await connection.BeginTransactionAsync(TestToken);

        // act
        var claimed = await ClaimAsync(connection, transaction);

        // assert
        Assert.Equal(inserted.Take(PostgresMessageOutboxQueries.BatchSize).ToList(), claimed);
    }

    [Fact]
    public async Task ProcessEvent_Should_ReturnNoRows_When_TransactionIsClaimedByAnotherConnection()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var inserted = await InsertAsync(connectionString, Guid.NewGuid(), 3);
        await using var connectionA = await OpenAsync(connectionString);
        await using var connectionB = await OpenAsync(connectionString);
        await using var transactionA = await connectionA.BeginTransactionAsync(TestToken);
        await using var transactionB = await connectionB.BeginTransactionAsync(TestToken);

        // act
        var claimedByA = await ClaimAsync(connectionA, transactionA);
        var claimedByB = await ClaimAsync(connectionB, transactionB);

        // assert
        Assert.Equal(inserted, claimedByA);
        Assert.Empty(claimedByB);
    }

    [Fact]
    public async Task ProcessEvent_Should_ClaimNextTransaction_When_FirstTransactionIsClaimedByAnotherConnection()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var first = await InsertAsync(connectionString, Guid.NewGuid(), 2);
        var second = await InsertAsync(connectionString, Guid.NewGuid(), 2);
        await using var connectionA = await OpenAsync(connectionString);
        await using var connectionB = await OpenAsync(connectionString);
        await using var transactionA = await connectionA.BeginTransactionAsync(TestToken);
        await using var transactionB = await connectionB.BeginTransactionAsync(TestToken);

        // act
        var claimedByA = await ClaimAsync(connectionA, transactionA);
        var claimedByB = await ClaimAsync(connectionB, transactionB);

        // assert
        Assert.Equal(first, claimedByA);
        Assert.Equal(second, claimedByB);
    }

    [Fact]
    public async Task ProcessEvent_Should_ClaimRowsOneByOne_When_RowsHaveNoTransactionId()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var first = await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(), TestToken);
        var second = await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(), TestToken);
        await using var connectionA = await OpenAsync(connectionString);
        await using var connectionB = await OpenAsync(connectionString);
        await using var transactionA = await connectionA.BeginTransactionAsync(TestToken);
        await using var transactionB = await connectionB.BeginTransactionAsync(TestToken);

        // act
        var claimedByA = await ClaimAsync(connectionA, transactionA);
        var claimedByB = await ClaimAsync(connectionB, transactionB);

        // assert
        Assert.Equal([first], claimedByA);
        Assert.Equal([second], claimedByB);
    }

    [Fact]
    public async Task ProcessEvent_Should_ReturnNoRows_When_FirstRowIsNotDue()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await InsertDelayedFirstRowAsync(connectionString);
        await using var connection = await OpenAsync(connectionString);
        await using var transaction = await connection.BeginTransactionAsync(TestToken);

        // act
        var claimed = await ClaimAsync(connection, transaction);

        // assert
        Assert.Empty(claimed);
    }

    [Fact]
    public async Task NextPollingInterval_Should_ReturnDelayOfFirstRow_When_LaterRowsAreDue()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await InsertDelayedFirstRowAsync(connectionString);
        await using var connection = await OpenAsync(connectionString);

        // act
        var seconds = await ScalarAsync(connection, s_queries.NextPollingInterval);

        // assert
        var value = Assert.IsType<double>(seconds);
        Assert.InRange(value, 1, 60);
    }

    [Fact]
    public async Task NextPollingInterval_Should_ReturnNull_When_FirstRowReachedAttemptLimit()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var transactionId = Guid.NewGuid();
        await MessageRows.InsertOutboxAsync(
            connectionString,
            CreateEnvelope(),
            transactionId,
            PostgresMessageOutboxQueries.MaxAttempts,
            TimeSpan.Zero,
            TestToken);
        await MessageRows.InsertOutboxAsync(
            connectionString, CreateEnvelope(), transactionId, 0, TimeSpan.Zero, TestToken);
        await using var connection = await OpenAsync(connectionString);
        await using var transaction = await connection.BeginTransactionAsync(TestToken);

        // act
        var claimed = await ClaimAsync(connection, transaction);
        await transaction.CommitAsync(TestToken);
        var seconds = await ScalarAsync(connection, s_queries.NextPollingInterval);

        // assert
        Assert.Empty(claimed);
        Assert.Equal(DBNull.Value, seconds);
    }

    [Fact]
    public async Task RescheduleEvent_Should_CountAttemptAndDelayRow_When_Executed()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var id = await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(), TestToken);
        await using var connection = await OpenAsync(connectionString);

        // act
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = s_queries.RescheduleEvent;
            command.Parameters.AddWithValue("@EventId", id);
            await command.ExecuteNonQueryAsync(TestToken);
        }

        await using var transaction = await connection.BeginTransactionAsync(TestToken);
        var claimed = await ClaimAsync(connection, transaction);

        // assert
        var rows = await MessageRows.ReadOutboxAsync(connectionString, TestToken);
        Assert.Equal([(id, null, 1)], rows);
        Assert.Empty(claimed);
    }

    [Fact]
    public async Task DeleteTransactionEvents_Should_DeleteRowsUpToId_When_Executed()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var transactionId = Guid.NewGuid();
        var inserted = await InsertAsync(connectionString, transactionId, 3);
        await using var connection = await OpenAsync(connectionString);

        // act
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = s_queries.DeleteTransactionEvents;
            command.Parameters.AddWithValue("@TransactionId", transactionId);
            command.Parameters.AddWithValue("@LastEventId", inserted[1]);
            await command.ExecuteNonQueryAsync(TestToken);
        }

        // assert
        var rows = await MessageRows.ReadOutboxAsync(connectionString, TestToken);
        Assert.Equal([(inserted[2], (Guid?)transactionId, 0)], rows);
    }

    private async Task<string> CreateDatabaseAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        var options = new DbContextOptionsBuilder<TestDbContext>().UseTestNpgsql(connectionString).Options;
        await using var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync(TestToken);
        return connectionString;
    }

    private static async Task<List<Guid>> InsertAsync(string connectionString, Guid transactionId, int count)
    {
        var ids = new List<Guid>(count);

        for (var i = 0; i < count; i++)
        {
            ids.Add(await MessageRows.InsertOutboxAsync(
                connectionString, CreateEnvelope(), transactionId, 0, TimeSpan.Zero, TestToken));
        }

        return ids;
    }

    private static async Task InsertDelayedFirstRowAsync(string connectionString)
    {
        var transactionId = Guid.NewGuid();
        await MessageRows.InsertOutboxAsync(
            connectionString, CreateEnvelope(), transactionId, 0, TimeSpan.FromMinutes(1), TestToken);
        await MessageRows.InsertOutboxAsync(
            connectionString, CreateEnvelope(), transactionId, 0, TimeSpan.Zero, TestToken);
        await MessageRows.InsertOutboxAsync(
            connectionString, CreateEnvelope(), transactionId, 0, TimeSpan.Zero, TestToken);
    }

    private static async Task<NpgsqlConnection> OpenAsync(string connectionString)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);
        return connection;
    }

    private static async Task<List<Guid>> ClaimAsync(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        var ids = new List<Guid>();

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = s_queries.ProcessEvent;

        await using var reader = await command.ExecuteReaderAsync(TestToken);
        while (await reader.ReadAsync(TestToken))
        {
            ids.Add(reader.GetGuid(0));
        }

        return ids;
    }

    private static async Task<object?> ScalarAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(TestToken);
    }

    private static MessageEnvelope CreateEnvelope()
        => new()
        {
            MessageId = Guid.NewGuid().ToString(),
            MessageType = "urn:message:test-event",
            DestinationAddress = "memory://test/queue"
        };
}
