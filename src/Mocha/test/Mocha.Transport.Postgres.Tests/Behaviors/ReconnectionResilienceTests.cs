using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mocha.Transport.Postgres.Tasks;
using Mocha.Transport.Postgres.Tests.Helpers;
using Npgsql;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public class ReconnectionResilienceTests
{
    private readonly PostgresFixture _fixture;

    public ReconnectionResilienceTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task HeartbeatAsync_Should_ReturnTrue_When_ConsumerAlive()
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
        var result = await consumerManager.HeartbeatAsync(CancellationToken.None);

        // assert
        Assert.True(result);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task HeartbeatAsync_Should_ReturnFalse_When_ConsumerEvicted()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var logger = NullLogger<PostgresConnectionManager>.Instance;
        var schemaOptions = new PostgresSchemaOptions();
        var connectionManager = new PostgresConnectionManager(db.ConnectionString, schemaOptions, logger);
        await connectionManager.EnsureMigratedAsync(CancellationToken.None);

        var consumerManager = new PostgresConsumerManager("test-service", connectionManager, schemaOptions);
        await consumerManager.RegisterAsync(CancellationToken.None);

        // Delete the consumer row to simulate eviction
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM mocha_consumers WHERE id = @id";
        cmd.Parameters.AddWithValue("id", consumerManager.ConsumerId);
        await cmd.ExecuteNonQueryAsync();

        // act
        var result = await consumerManager.HeartbeatAsync(CancellationToken.None);

        // assert
        Assert.False(result);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task Consumer_Should_ReRegisterAndReprovision_When_Evicted()
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

        await using var insertQueueCmd = conn.CreateCommand();
        insertQueueCmd.CommandText = """
            INSERT INTO mocha_queue (name, consumer_id)
            VALUES ('temp-reply-queue', @consumerId)
            """;
        insertQueueCmd.Parameters.AddWithValue("consumerId", consumerManager.ConsumerId);
        await insertQueueCmd.ExecuteNonQueryAsync();

        // Delete the consumer row (simulates eviction by ExpiredConsumerCleanupTask)
        // This CASCADE deletes the temp queue too
        await using var deleteCmd = conn.CreateCommand();
        deleteCmd.CommandText = "DELETE FROM mocha_consumers WHERE id = @id";
        deleteCmd.Parameters.AddWithValue("id", consumerManager.ConsumerId);
        await deleteCmd.ExecuteNonQueryAsync();

        // Verify both are gone
        await using var checkConsumer = conn.CreateCommand();
        checkConsumer.CommandText = "SELECT COUNT(*) FROM mocha_consumers WHERE id = @id";
        checkConsumer.Parameters.AddWithValue("id", consumerManager.ConsumerId);
        Assert.Equal(0L, (long)(await checkConsumer.ExecuteScalarAsync())!);

        await using var checkQueue = conn.CreateCommand();
        checkQueue.CommandText = "SELECT COUNT(*) FROM mocha_queue WHERE name = 'temp-reply-queue'";
        Assert.Equal(0L, (long)(await checkQueue.ExecuteScalarAsync())!);

        // act - recover: re-register consumer and re-provision queue
        await consumerManager.RegisterAsync(CancellationToken.None);

        await using var reprovisionCmd = conn.CreateCommand();
        reprovisionCmd.CommandText = """
            INSERT INTO mocha_queue (name, consumer_id)
            VALUES ('temp-reply-queue', @consumerId)
            ON CONFLICT (name) DO UPDATE SET consumer_id = @consumerId
            """;
        reprovisionCmd.Parameters.AddWithValue("consumerId", consumerManager.ConsumerId);
        await reprovisionCmd.ExecuteNonQueryAsync();

        // assert - both are re-created
        await using var verifyConsumer = conn.CreateCommand();
        verifyConsumer.CommandText = "SELECT COUNT(*) FROM mocha_consumers WHERE id = @id";
        verifyConsumer.Parameters.AddWithValue("id", consumerManager.ConsumerId);
        Assert.Equal(1L, (long)(await verifyConsumer.ExecuteScalarAsync())!);

        await using var verifyQueue = conn.CreateCommand();
        verifyQueue.CommandText = "SELECT COUNT(*) FROM mocha_queue WHERE name = 'temp-reply-queue'";
        Assert.Equal(1L, (long)(await verifyQueue.ExecuteScalarAsync())!);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task OrphanedMessages_Should_BeReleased_When_LockExpired()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var logger = NullLogger<PostgresConnectionManager>.Instance;
        var schemaOptions = new PostgresSchemaOptions();
        var connectionManager = new PostgresConnectionManager(db.ConnectionString, schemaOptions, logger);
        await connectionManager.EnsureMigratedAsync(CancellationToken.None);

        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();

        // Create a queue
        await using var createQueue = conn.CreateCommand();
        createQueue.CommandText = "INSERT INTO mocha_queue (name) VALUES ('test-queue') RETURNING id";
        var queueId = (long)(await createQueue.ExecuteScalarAsync())!;

        // Insert a message with consumer_id set and last_delivered > 5 minutes ago
        var consumerId = Guid.NewGuid();
        await using var insertMsg = conn.CreateCommand();
        insertMsg.CommandText = """
            INSERT INTO mocha_message (body, queue_id, consumer_id, last_delivered, delivery_count)
            VALUES (@body, @queueId, @consumerId, now() AT TIME ZONE 'utc' - INTERVAL '10 minutes', 1)
            RETURNING transport_message_id
            """;
        insertMsg.Parameters.AddWithValue("body", new byte[] { 1, 2, 3 });
        insertMsg.Parameters.AddWithValue("queueId", queueId);
        insertMsg.Parameters.AddWithValue("consumerId", consumerId);
        var messageId = (Guid)(await insertMsg.ExecuteScalarAsync())!;

        // act - run orphaned message cleanup
        var cleanupTask = new OrphanedMessageCleanupTask(connectionManager, schemaOptions, NullLogger.Instance);
        // Use reflection to call ExecuteAsync since it's protected
        var method = typeof(OrphanedMessageCleanupTask).GetMethod(
            "ExecuteAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method!.Invoke(cleanupTask, [CancellationToken.None])!;

        // assert - consumer_id should be cleared
        await using var checkMsg = conn.CreateCommand();
        checkMsg.CommandText = "SELECT consumer_id FROM mocha_message WHERE transport_message_id = @id";
        checkMsg.Parameters.AddWithValue("id", messageId);
        var result = await checkMsg.ExecuteScalarAsync();
        Assert.True(result is DBNull, "consumer_id should be NULL after orphaned cleanup");

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task OrphanedMessages_Should_NotBeReleased_When_LockRecent()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var logger = NullLogger<PostgresConnectionManager>.Instance;
        var schemaOptions = new PostgresSchemaOptions();
        var connectionManager = new PostgresConnectionManager(db.ConnectionString, schemaOptions, logger);
        await connectionManager.EnsureMigratedAsync(CancellationToken.None);

        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();

        // Create a queue
        await using var createQueue = conn.CreateCommand();
        createQueue.CommandText = "INSERT INTO mocha_queue (name) VALUES ('test-queue') RETURNING id";
        var queueId = (long)(await createQueue.ExecuteScalarAsync())!;

        // Insert a message with consumer_id set and last_delivered = now (recent lock)
        var consumerId = Guid.NewGuid();
        await using var insertMsg = conn.CreateCommand();
        insertMsg.CommandText = """
            INSERT INTO mocha_message (body, queue_id, consumer_id, last_delivered, delivery_count)
            VALUES (@body, @queueId, @consumerId, now() AT TIME ZONE 'utc', 1)
            RETURNING transport_message_id
            """;
        insertMsg.Parameters.AddWithValue("body", new byte[] { 1, 2, 3 });
        insertMsg.Parameters.AddWithValue("queueId", queueId);
        insertMsg.Parameters.AddWithValue("consumerId", consumerId);
        var messageId = (Guid)(await insertMsg.ExecuteScalarAsync())!;

        // act - run orphaned message cleanup
        var cleanupTask = new OrphanedMessageCleanupTask(connectionManager, schemaOptions, NullLogger.Instance);
        var method = typeof(OrphanedMessageCleanupTask).GetMethod(
            "ExecuteAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method!.Invoke(cleanupTask, [CancellationToken.None])!;

        // assert - consumer_id should still be set
        await using var checkMsg = conn.CreateCommand();
        checkMsg.CommandText = "SELECT consumer_id FROM mocha_message WHERE transport_message_id = @id";
        checkMsg.Parameters.AddWithValue("id", messageId);
        var result = await checkMsg.ExecuteScalarAsync();
        Assert.Equal(consumerId, result);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task ReleasedOrphanedMessages_Should_BeAvailableForRedelivery()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var logger = NullLogger<PostgresConnectionManager>.Instance;
        var schemaOptions = new PostgresSchemaOptions();
        var connectionManager = new PostgresConnectionManager(db.ConnectionString, schemaOptions, logger);
        await connectionManager.EnsureMigratedAsync(CancellationToken.None);

        var messageStore = new PostgresMessageStore(connectionManager, schemaOptions);

        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();

        // Create a queue
        await using var createQueue = conn.CreateCommand();
        createQueue.CommandText = "INSERT INTO mocha_queue (name) VALUES ('test-queue') RETURNING id";
        var queueId = (long)(await createQueue.ExecuteScalarAsync())!;

        // Insert a message locked by a now-dead consumer > 5 min ago
        var deadConsumerId = Guid.NewGuid();
        await using var insertMsg = conn.CreateCommand();
        insertMsg.CommandText = """
            INSERT INTO mocha_message (body, queue_id, consumer_id, last_delivered, delivery_count)
            VALUES (@body, @queueId, @consumerId, now() AT TIME ZONE 'utc' - INTERVAL '10 minutes', 1)
            """;
        insertMsg.Parameters.AddWithValue("body", new byte[] { 42 });
        insertMsg.Parameters.AddWithValue("queueId", queueId);
        insertMsg.Parameters.AddWithValue("consumerId", deadConsumerId);
        await insertMsg.ExecuteNonQueryAsync();

        // Verify message is not readable (consumer_id is set)
        var newConsumerId = Guid.NewGuid();
        using var beforeBatch = await messageStore.ReadMessagesAsync(
            10,
            "test-queue",
            newConsumerId,
            CancellationToken.None);
        Assert.Empty(beforeBatch.Messages);

        // act - run orphaned message cleanup
        var cleanupTask = new OrphanedMessageCleanupTask(connectionManager, schemaOptions, NullLogger.Instance);
        var method = typeof(OrphanedMessageCleanupTask).GetMethod(
            "ExecuteAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method!.Invoke(cleanupTask, [CancellationToken.None])!;

        // assert - message should now be readable
        using var afterBatch = await messageStore.ReadMessagesAsync(
            10,
            "test-queue",
            newConsumerId,
            CancellationToken.None);
        Assert.Single(afterBatch.Messages);
        Assert.Equal(new byte[] { 42 }, afterBatch.Messages[0].Body.ToArray());

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task NotificationListener_Should_Reconnect_When_ConnectionDropped()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var connManagerLogger = NullLogger<PostgresConnectionManager>.Instance;
        var listenerLogger = NullLoggerFactory.Instance.CreateLogger<PostgresNotificationListener>();
        var schemaOptions = new PostgresSchemaOptions();
        var connectionManager = new PostgresConnectionManager(db.ConnectionString, schemaOptions, connManagerLogger);
        await connectionManager.EnsureMigratedAsync(CancellationToken.None);

        var listener = new PostgresNotificationListener(connectionManager, schemaOptions, listenerLogger);
        var receivedPayloads = new List<string>();
        var signal = new SemaphoreSlim(0);
        var reconnectedSignal = new SemaphoreSlim(0);

        listener.Subscribe(payload =>
        {
            receivedPayloads.Add(payload);
            if (string.IsNullOrEmpty(payload))
            {
                reconnectedSignal.Release();
            }
            else
            {
                signal.Release();
            }
        });

        await listener.StartAsync(CancellationToken.None);

        // Verify listener works before disconnect
        await using (var conn = new NpgsqlConnection(db.ConnectionString))
        {
            await conn.OpenAsync();
            await using var notifyCmd = conn.CreateCommand();
            notifyCmd.CommandText = $"SELECT pg_notify('{schemaOptions.NotificationChannel}', 'test-queue')";
            await notifyCmd.ExecuteNonQueryAsync();
        }

        Assert.True(await signal.WaitAsync(TimeSpan.FromSeconds(5)), "Should receive notification before disconnect");
        Assert.Contains("test-queue", receivedPayloads);

        // act - kill the LISTEN connection via pg_terminate_backend
        await using (var adminConn = new NpgsqlConnection(db.ConnectionString))
        {
            await adminConn.OpenAsync();
            await using var killCmd = adminConn.CreateCommand();
            killCmd.CommandText = $"""
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = '{db.DatabaseName}'
                  AND pid <> pg_backend_pid()
                  AND query LIKE '%LISTEN%'
                """;
            await killCmd.ExecuteNonQueryAsync();
        }

        // Wait for the listener to reconnect (it broadcasts an empty-string payload on reconnect)
        Assert.True(
            await reconnectedSignal.WaitAsync(TimeSpan.FromSeconds(15)),
            "Listener did not reconnect within timeout");

        // Send a notification after reconnection
        receivedPayloads.Clear();
        await using (var conn2 = new NpgsqlConnection(db.ConnectionString))
        {
            await conn2.OpenAsync();
            await using var notifyCmd2 = conn2.CreateCommand();
            notifyCmd2.CommandText = $"SELECT pg_notify('{schemaOptions.NotificationChannel}', 'after-reconnect')";
            await notifyCmd2.ExecuteNonQueryAsync();
        }

        // assert - should receive notification after reconnection
        Assert.True(await signal.WaitAsync(TimeSpan.FromSeconds(10)), "Should receive notification after reconnect");
        Assert.Contains("after-reconnect", receivedPayloads);

        await listener.DisposeAsync();
        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task NotificationListener_Should_SignalSubscribers_When_Reconnected()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var connManagerLogger = NullLogger<PostgresConnectionManager>.Instance;
        var listenerLogger = NullLoggerFactory.Instance.CreateLogger<PostgresNotificationListener>();
        var schemaOptions = new PostgresSchemaOptions();
        var connectionManager = new PostgresConnectionManager(db.ConnectionString, schemaOptions, connManagerLogger);
        await connectionManager.EnsureMigratedAsync(CancellationToken.None);

        var listener = new PostgresNotificationListener(connectionManager, schemaOptions, listenerLogger);
        var emptyPayloads = new List<string>();
        var reconnectSignal = new SemaphoreSlim(0);

        listener.Subscribe(payload =>
        {
            if (string.IsNullOrEmpty(payload))
            {
                emptyPayloads.Add(payload);
                reconnectSignal.Release();
            }
        });

        await listener.StartAsync(CancellationToken.None);

        // act - kill the LISTEN connection to trigger reconnect
        await using (var adminConn = new NpgsqlConnection(db.ConnectionString))
        {
            await adminConn.OpenAsync();
            await using var killCmd = adminConn.CreateCommand();
            killCmd.CommandText = $"""
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = '{db.DatabaseName}'
                  AND pid <> pg_backend_pid()
                  AND query LIKE '%LISTEN%'
                """;
            await killCmd.ExecuteNonQueryAsync();
        }

        // assert - empty-string signal should be sent to all subscribers on reconnect
        Assert.True(
            await reconnectSignal.WaitAsync(TimeSpan.FromSeconds(10)),
            "Should receive empty-payload signal after reconnect");
        Assert.Contains(string.Empty, emptyPayloads);

        await listener.DisposeAsync();
        await connectionManager.DisposeAsync();
    }
}
