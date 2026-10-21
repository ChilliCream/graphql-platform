using Microsoft.EntityFrameworkCore;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxQueriesTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData(0, true, 0)]
    [InlineData(1, false, 86400)]
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
