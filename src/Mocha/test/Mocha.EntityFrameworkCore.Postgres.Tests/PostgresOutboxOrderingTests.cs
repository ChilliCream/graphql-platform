using Microsoft.EntityFrameworkCore;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Npgsql;
using NpgsqlTypes;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxOrderingTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ProcessEvent_Should_SkipEntireGroup_When_AnotherWorkerLocksItsHead()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var group = Guid.NewGuid();
        var head = await InsertAsync(connectionString, group, 0);
        await InsertAsync(connectionString, group, 1);
        var independent = await InsertAsync(connectionString, null, 0);
        await using var first = new NpgsqlConnection(connectionString);
        await using var second = new NpgsqlConnection(connectionString);
        await first.OpenAsync(TestToken);
        await second.OpenAsync(TestToken);
        await using var transaction = await first.BeginTransactionAsync(TestToken);

        // act
        var firstResult = await ProcessAsync(first, transaction);
        var secondResult = await ProcessAsync(second);
        var thirdResult = await ProcessAsync(second);

        // assert
        Assert.Equal(head, firstResult);
        Assert.Equal(independent, secondResult);
        Assert.Null(thirdResult);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(PostgresMessageOutboxQueries.MaxAttempts)]
    public async Task ProcessEvent_Should_BlockLaterMessages_When_GroupHeadIsRetryingOrExhausted(int attempts)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var group = Guid.NewGuid();
        await InsertAsync(connectionString, group, 0, attempts);
        await InsertAsync(connectionString, group, 1);
        var independent = await InsertAsync(connectionString, null, 0);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);

        // act
        var firstResult = await ProcessAsync(connection);
        await DeleteAsync(connection, independent);
        var secondResult = await ProcessAsync(connection);

        // assert
        Assert.Equal(independent, firstResult);
        Assert.Null(secondResult);
    }

    [Fact]
    public async Task ProcessEvent_Should_AdvanceGroup_When_AcknowledgedHeadIsDeleted()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var group = Guid.NewGuid();
        var head = await InsertAsync(connectionString, group, 0);
        var next = await InsertAsync(connectionString, group, 1);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);

        // act
        var firstResult = await ProcessAsync(connection);
        await DeleteAsync(connection, head);
        var secondResult = await ProcessAsync(connection);

        // assert
        Assert.Equal(head, firstResult);
        Assert.Equal(next, secondResult);
    }

    [Fact]
    public async Task ProcessEvent_Should_BlockGroupUntilDue_When_HeadHasScheduledTime()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var group = Guid.NewGuid();
        var scheduled = await InsertAsync(connectionString, group, 0, scheduled: true);
        var follower = await InsertAsync(connectionString, group, 1);
        var independent = await InsertAsync(connectionString, null, 0);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);

        // act
        var beforeDue = await ProcessAsync(connection);
        await DeleteAsync(connection, independent);
        var blocked = await ProcessAsync(connection);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE outbox_messages SET scheduled_at = NOW() WHERE id = @id";
        command.Parameters.AddWithValue("id", scheduled);
        await command.ExecuteNonQueryAsync(TestToken);
        var afterDue = await ProcessAsync(connection);
        await DeleteAsync(connection, scheduled);
        var afterHead = await ProcessAsync(connection);

        // assert
        Assert.Equal(independent, beforeDue);
        Assert.Null(blocked);
        Assert.Equal(scheduled, afterDue);
        Assert.Equal(follower, afterHead);
    }

    [Fact]
    public async Task ProcessEvent_Should_RetrySameHead_When_DeliveryTransactionRollsBack()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var group = Guid.NewGuid();
        var head = await InsertAsync(connectionString, group, 0);
        await InsertAsync(connectionString, group, 1);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);

        // act
        Guid? beforeRollback;
        await using (var transaction = await connection.BeginTransactionAsync(TestToken))
        {
            beforeRollback = await ProcessAsync(connection, transaction);
            await transaction.RollbackAsync(TestToken);
        }

        var afterRollback = await ProcessAsync(connection);

        // assert
        Assert.Equal(head, beforeRollback);
        Assert.Equal(head, afterRollback);
    }

    private async Task<string> CreateDatabaseAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        var options = new DbContextOptionsBuilder<TestDbContext>().UseTestNpgsql(connectionString).Options;
        await using var db = new TestDbContext(options);
        await db.Database.EnsureCreatedAsync(TestToken);
        return connectionString;
    }

    private static async Task<Guid> InsertAsync(
        string connectionString,
        Guid? group,
        long sequence,
        int attempts = 0,
        bool scheduled = false)
    {
        var id = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO outbox_messages (id, envelope, times_sent, created_at, group_id, sequence, scheduled_at)
            VALUES (@id, '{}'::json, @attempts,
                CASE WHEN @attempts = 0 THEN clock_timestamp() ELSE NOW() + INTERVAL '1 day' END,
                @group, @sequence, CASE WHEN @scheduled THEN NOW() + INTERVAL '1 day' ELSE NULL END)
            """;
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("attempts", attempts);
        command.Parameters.AddWithValue("group", NpgsqlDbType.Uuid, (object?)group ?? DBNull.Value);
        command.Parameters.AddWithValue("sequence", sequence);
        command.Parameters.AddWithValue("scheduled", scheduled);
        await command.ExecuteNonQueryAsync(TestToken);
        return id;
    }

    private static async Task<Guid?> ProcessAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = PostgresMessageOutboxQueries.From(new OutboxTableInfo()).ProcessEvent;
        return (Guid?)await command.ExecuteScalarAsync(TestToken);
    }

    private static async Task DeleteAsync(NpgsqlConnection connection, Guid id)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM outbox_messages WHERE id = @id";
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(TestToken);
    }
}
