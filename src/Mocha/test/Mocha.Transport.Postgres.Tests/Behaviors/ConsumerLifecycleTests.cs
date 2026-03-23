using Microsoft.Extensions.Logging.Abstractions;
using Mocha.Transport.Postgres.Tests.Helpers;
using Npgsql;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public class ConsumerLifecycleTests
{
    private readonly PostgresFixture _fixture;

    public ConsumerLifecycleTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RegisterAsync_Should_InsertConsumer_When_Called()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var logger = NullLogger<PostgresConnectionManager>.Instance;
        var schemaOptions = new PostgresSchemaOptions();
        var connectionManager = new PostgresConnectionManager(db.ConnectionString, schemaOptions, logger);
        await connectionManager.EnsureMigratedAsync(CancellationToken.None);

        var consumerManager = new PostgresConsumerManager("test-service", connectionManager, schemaOptions);

        // act
        await consumerManager.RegisterAsync(CancellationToken.None);

        // assert
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM mocha_consumers WHERE id = @id";
        cmd.Parameters.AddWithValue("id", consumerManager.ConsumerId);
        var count = (long)(await cmd.ExecuteScalarAsync())!;
        Assert.Equal(1, count);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task HeartbeatAsync_Should_UpdateTimestamp_When_Called()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var logger = NullLogger<PostgresConnectionManager>.Instance;
        var schemaOptions = new PostgresSchemaOptions();
        var connectionManager = new PostgresConnectionManager(db.ConnectionString, schemaOptions, logger);
        await connectionManager.EnsureMigratedAsync(CancellationToken.None);

        var consumerManager = new PostgresConsumerManager("test-service", connectionManager, schemaOptions);
        await consumerManager.RegisterAsync(CancellationToken.None);

        // get initial timestamp
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();
        await using var cmd1 = conn.CreateCommand();
        cmd1.CommandText = "SELECT updated_at FROM mocha_consumers WHERE id = @id";
        cmd1.Parameters.AddWithValue("id", consumerManager.ConsumerId);
        var initialTime = (DateTime)(await cmd1.ExecuteScalarAsync())!;

        await Task.Delay(50); // Ensure clock advances

        // act
        await consumerManager.HeartbeatAsync(CancellationToken.None);

        // assert
        await using var cmd2 = conn.CreateCommand();
        cmd2.CommandText = "SELECT updated_at FROM mocha_consumers WHERE id = @id";
        cmd2.Parameters.AddWithValue("id", consumerManager.ConsumerId);
        var updatedTime = (DateTime)(await cmd2.ExecuteScalarAsync())!;

        Assert.True(updatedTime >= initialTime, "Heartbeat should update the timestamp");

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task UnregisterAsync_Should_DeleteConsumer_When_Called()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var logger = NullLogger<PostgresConnectionManager>.Instance;
        var schemaOptions = new PostgresSchemaOptions();
        var connectionManager = new PostgresConnectionManager(db.ConnectionString, schemaOptions, logger);
        await connectionManager.EnsureMigratedAsync(CancellationToken.None);

        var consumerManager = new PostgresConsumerManager("test-service", connectionManager, schemaOptions);
        await consumerManager.RegisterAsync(CancellationToken.None);

        // act
        await consumerManager.UnregisterAsync(CancellationToken.None);

        // assert
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM mocha_consumers WHERE id = @id";
        cmd.Parameters.AddWithValue("id", consumerManager.ConsumerId);
        var count = (long)(await cmd.ExecuteScalarAsync())!;
        Assert.Equal(0, count);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task CleanupExpiredConsumersAsync_Should_DeleteExpiredConsumers_When_TimedOut()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var logger = NullLogger<PostgresConnectionManager>.Instance;
        var schemaOptions = new PostgresSchemaOptions();
        var connectionManager = new PostgresConnectionManager(db.ConnectionString, schemaOptions, logger);
        await connectionManager.EnsureMigratedAsync(CancellationToken.None);

        var consumerManager = new PostgresConsumerManager("test-service", connectionManager, schemaOptions);
        await consumerManager.RegisterAsync(CancellationToken.None);

        // Set the updated_at to the past to simulate expiration
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();
        await using var updateCmd = conn.CreateCommand();
        updateCmd.CommandText = "UPDATE mocha_consumers SET updated_at = now() - INTERVAL '5 minutes' WHERE id = @id";
        updateCmd.Parameters.AddWithValue("id", consumerManager.ConsumerId);
        await updateCmd.ExecuteNonQueryAsync();

        // act
        await consumerManager.CleanupExpiredConsumersAsync(
            TimeSpan.FromMinutes(2),
            CancellationToken.None);

        // assert
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM mocha_consumers WHERE id = @id";
        cmd.Parameters.AddWithValue("id", consumerManager.ConsumerId);
        var count = (long)(await cmd.ExecuteScalarAsync())!;
        Assert.Equal(0, count);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task CleanupExpiredConsumersAsync_Should_NotDeleteActiveConsumers_When_RecentHeartbeat()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var logger = NullLogger<PostgresConnectionManager>.Instance;
        var schemaOptions = new PostgresSchemaOptions();
        var connectionManager = new PostgresConnectionManager(db.ConnectionString, schemaOptions, logger);
        await connectionManager.EnsureMigratedAsync(CancellationToken.None);

        var consumerManager = new PostgresConsumerManager("test-service", connectionManager, schemaOptions);
        await consumerManager.RegisterAsync(CancellationToken.None);

        // act - cleanup with 2 minute timeout, consumer was just registered
        await consumerManager.CleanupExpiredConsumersAsync(
            TimeSpan.FromMinutes(2),
            CancellationToken.None);

        // assert
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM mocha_consumers WHERE id = @id";
        cmd.Parameters.AddWithValue("id", consumerManager.ConsumerId);
        var count = (long)(await cmd.ExecuteScalarAsync())!;
        Assert.Equal(1, count);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task UnregisterAsync_Should_CascadeDeleteTempQueues_When_ConsumerDeleted()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var logger = NullLogger<PostgresConnectionManager>.Instance;
        var schemaOptions = new PostgresSchemaOptions();
        var connectionManager = new PostgresConnectionManager(db.ConnectionString, schemaOptions, logger);
        await connectionManager.EnsureMigratedAsync(CancellationToken.None);

        var consumerManager = new PostgresConsumerManager("test-service", connectionManager, schemaOptions);
        await consumerManager.RegisterAsync(CancellationToken.None);

        // Create a temp queue linked to this consumer
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();
        await using var insertCmd = conn.CreateCommand();
        insertCmd.CommandText = """
            INSERT INTO mocha_queue (name, consumer_id)
            VALUES ('temp-reply-queue', @consumerId)
            """;
        insertCmd.Parameters.AddWithValue("consumerId", consumerManager.ConsumerId);
        await insertCmd.ExecuteNonQueryAsync();

        // Verify queue exists
        await using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM mocha_queue WHERE name = 'temp-reply-queue'";
        var beforeCount = (long)(await checkCmd.ExecuteScalarAsync())!;
        Assert.Equal(1, beforeCount);

        // act
        await consumerManager.UnregisterAsync(CancellationToken.None);

        // assert - queue should be cascade deleted
        await using var afterCmd = conn.CreateCommand();
        afterCmd.CommandText = "SELECT COUNT(*) FROM mocha_queue WHERE name = 'temp-reply-queue'";
        var afterCount = (long)(await afterCmd.ExecuteScalarAsync())!;
        Assert.Equal(0, afterCount);

        await connectionManager.DisposeAsync();
    }
}
