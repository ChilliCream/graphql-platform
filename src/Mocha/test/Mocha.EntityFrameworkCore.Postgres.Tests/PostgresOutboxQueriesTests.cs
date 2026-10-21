using Microsoft.EntityFrameworkCore;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Npgsql;
using NpgsqlTypes;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxQueriesTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Queries_Should_ProcessMessages_When_UsingExistingSchema()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE outbox_messages (
                id uuid PRIMARY KEY,
                envelope json NOT NULL,
                times_sent integer NOT NULL,
                created_at timestamp with time zone NOT NULL
            )
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
        var queries = PostgresMessageOutboxQueries.From(new OutboxTableInfo());
        var id = Guid.NewGuid();

        // act
        command.CommandText = queries.InsertEnvelope;
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("envelope", NpgsqlDbType.Json, "{}");
        var inserted = await command.ExecuteNonQueryAsync(cancellationToken);
        command.Parameters.Clear();
        command.CommandText = queries.NextPollingInterval;
        var nextPoll = await command.ExecuteScalarAsync(cancellationToken);
        command.CommandText = queries.ProcessEvent;
        var processedId = await command.ExecuteScalarAsync(cancellationToken);
        command.CommandText = queries.DeleteEvent;
        command.Parameters.AddWithValue("EventId", id);
        var deleted = await command.ExecuteNonQueryAsync(cancellationToken);
        command.Parameters.Clear();
        command.CommandText = queries.NextPollingInterval;
        var nextPollAfterDelete = await command.ExecuteScalarAsync(cancellationToken);

        // assert
        Assert.Equal(1, inserted);
        Assert.IsType<DateTime>(nextPoll);
        Assert.Equal(id, processedId);
        Assert.Equal(1, deleted);
        Assert.Equal(DBNull.Value, nextPollAfterDelete);
    }

    [Theory]
    [InlineData(0, true, 0)]
    [InlineData(1, false, 86402)]
    public async Task ProcessEvent_Should_OnlyDelayRetries_When_CreatedAtIsInTheFuture(
        int attempts,
        bool ready,
        int pollingDelaySeconds)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionString = await fixture.CreateDatabaseAsync();
        var options = new DbContextOptionsBuilder<TestDbContext>().UseTestNpgsql(connectionString).Options;
        await using var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync(cancellationToken);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT NOW()";
        var now = (DateTime)(await command.ExecuteScalarAsync(cancellationToken))!;
        var id = Guid.NewGuid();
        command.CommandText = """
            INSERT INTO outbox_messages (id, envelope, times_sent, created_at)
            VALUES (@id, '{}'::json, @attempts, NOW() + INTERVAL '1 day')
            """;
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("attempts", attempts);
        await command.ExecuteNonQueryAsync(cancellationToken);
        command.Parameters.Clear();
        var queries = PostgresMessageOutboxQueries.From(new OutboxTableInfo());

        // act
        command.CommandText = queries.NextPollingInterval;
        var nextPoll = await command.ExecuteScalarAsync(cancellationToken);
        command.CommandText = queries.ProcessEvent;
        var processedId = await command.ExecuteScalarAsync(cancellationToken);

        // assert
        Assert.Equal(now.AddSeconds(pollingDelaySeconds), nextPoll);
        Assert.Equal(ready ? id : null, processedId);
    }
}
