using Microsoft.Extensions.Logging.Abstractions;
using Mocha.Transport.Postgres.Tests.Helpers;
using Npgsql;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public class MessageStoreTests
{
    private readonly PostgresFixture _fixture;

    public MessageStoreTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SendAsync_Should_InsertMessage_When_QueueExists()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var (connectionManager, messageStore) = await CreateStoreAsync(db);
        await CreateQueueAsync(db, "test-queue");

        // act
        await messageStore.SendAsync(
            "{\"key\":\"value\"}"u8.ToArray(),
            """{"messageId":"msg-1"}"""u8.ToArray(),
            "test-queue",
            CancellationToken.None);

        // assert
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM mocha_message m
            JOIN mocha_queue q ON m.queue_id = q.id
            WHERE q.name = 'test-queue'
            """;
        var count = (long)(await cmd.ExecuteScalarAsync())!;
        Assert.Equal(1, count);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task ReadMessagesAsync_Should_ReturnMessages_When_MessagesExist()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var (connectionManager, messageStore) = await CreateStoreAsync(db);
        await CreateQueueAsync(db, "read-queue");
        var consumerId = Guid.NewGuid();
        await RegisterConsumerAsync(db, consumerId);

        await messageStore.SendAsync("{}"u8.ToArray(), null, "read-queue", CancellationToken.None);

        // act
        using var batch = await messageStore.ReadMessagesAsync(10, "read-queue", consumerId, CancellationToken.None);

        // assert
        Assert.Single(batch.Messages);
        Assert.Equal(1, batch.Messages[0].DeliveryCount);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task DeleteMessageAsync_Should_RemoveMessage_When_MessageExists()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var (connectionManager, messageStore) = await CreateStoreAsync(db);
        await CreateQueueAsync(db, "del-queue");
        var consumerId = Guid.NewGuid();
        await RegisterConsumerAsync(db, consumerId);

        await messageStore.SendAsync("{}"u8.ToArray(), null, "del-queue", CancellationToken.None);
        using var batch = await messageStore.ReadMessagesAsync(10, "del-queue", consumerId, CancellationToken.None);
        var messageId = batch.Messages[0].TransportMessageId;

        // act
        await messageStore.DeleteMessageAsync(messageId, CancellationToken.None);

        // assert
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM mocha_message WHERE transport_message_id = @id";
        cmd.Parameters.AddWithValue("id", messageId);
        var count = (long)(await cmd.ExecuteScalarAsync())!;
        Assert.Equal(0, count);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task ReleaseMessageAsync_Should_ClearConsumer_When_Called()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var (connectionManager, messageStore) = await CreateStoreAsync(db);
        await CreateQueueAsync(db, "release-queue");
        var consumerId = Guid.NewGuid();
        await RegisterConsumerAsync(db, consumerId);

        await messageStore.SendAsync("{}"u8.ToArray(), null, "release-queue", CancellationToken.None);
        using var batch = await messageStore.ReadMessagesAsync(10, "release-queue", consumerId, CancellationToken.None);
        var messageId = batch.Messages[0].TransportMessageId;

        // act
        await messageStore.ReleaseMessageAsync(messageId, CancellationToken.None);

        // assert - message should be available again (consumer_id cleared)
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT consumer_id FROM mocha_message WHERE transport_message_id = @id";
        cmd.Parameters.AddWithValue("id", messageId);
        var result = await cmd.ExecuteScalarAsync();
        Assert.True(result is DBNull, "consumer_id should be null after release");

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task ReleaseMessageAsync_Should_RecordErrorInfo_When_ErrorInfoProvided()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var (connectionManager, messageStore) = await CreateStoreAsync(db);
        await CreateQueueAsync(db, "error-queue");
        var consumerId = Guid.NewGuid();
        await RegisterConsumerAsync(db, consumerId);

        await messageStore.SendAsync("{}"u8.ToArray(), null, "error-queue", CancellationToken.None);
        using var batch = await messageStore.ReadMessagesAsync(10, "error-queue", consumerId, CancellationToken.None);
        var messageId = batch.Messages[0].TransportMessageId;

        var errorInfo = new ErrorInfo("TestException", "Something went wrong", "at Test.Method()");

        // act
        await messageStore.ReleaseMessageAsync(messageId, errorInfo, CancellationToken.None);

        // assert
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT error_reason::text FROM mocha_message WHERE transport_message_id = @id";
        cmd.Parameters.AddWithValue("id", messageId);
        var errorReasonJson = (string?)(await cmd.ExecuteScalarAsync());
        Assert.NotNull(errorReasonJson);
        Assert.Contains("TestException", errorReasonJson);
        Assert.Contains("Something went wrong", errorReasonJson);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task UpdateErrorReasonAsync_Should_AccumulateErrors_When_CalledMultipleTimes()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var (connectionManager, messageStore) = await CreateStoreAsync(db);
        await CreateQueueAsync(db, "accum-queue");
        var consumerId = Guid.NewGuid();
        await RegisterConsumerAsync(db, consumerId);

        await messageStore.SendAsync("{}"u8.ToArray(), null, "accum-queue", CancellationToken.None);
        using var batch = await messageStore.ReadMessagesAsync(10, "accum-queue", consumerId, CancellationToken.None);
        var messageId = batch.Messages[0].TransportMessageId;

        var error1 = new ErrorInfo("Error1", "First failure", null);
        var error2 = new ErrorInfo("Error2", "Second failure", null);

        // act
        await messageStore.UpdateErrorReasonAsync(messageId, error1, CancellationToken.None);
        await messageStore.UpdateErrorReasonAsync(messageId, error2, CancellationToken.None);

        // assert
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT error_reason::text FROM mocha_message WHERE transport_message_id = @id";
        cmd.Parameters.AddWithValue("id", messageId);
        var errorReasonJson = (string?)(await cmd.ExecuteScalarAsync());
        Assert.NotNull(errorReasonJson);
        Assert.Contains("Error1", errorReasonJson);
        Assert.Contains("Error2", errorReasonJson);
        Assert.Contains("First failure", errorReasonJson);
        Assert.Contains("Second failure", errorReasonJson);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task PublishAsync_Should_FanOut_When_MultipleSubscriptions()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var (connectionManager, messageStore) = await CreateStoreAsync(db);
        await CreateTopicAsync(db, "events");
        await CreateQueueAsync(db, "sub-1");
        await CreateQueueAsync(db, "sub-2");
        await CreateSubscriptionAsync(db, "events", "sub-1");
        await CreateSubscriptionAsync(db, "events", "sub-2");

        // act
        await messageStore.PublishAsync("{}"u8.ToArray(), null, "events", CancellationToken.None);

        // assert
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM mocha_message";
        var count = (long)(await cmd.ExecuteScalarAsync())!;
        Assert.Equal(2, count);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task ReadMessagesAsync_Should_IncludeMaxDeliveryCount_When_MessageRead()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var (connectionManager, messageStore) = await CreateStoreAsync(db);
        await CreateQueueAsync(db, "maxdc-queue");
        var consumerId = Guid.NewGuid();
        await RegisterConsumerAsync(db, consumerId);

        await messageStore.SendAsync("{}"u8.ToArray(), null, "maxdc-queue", CancellationToken.None);

        // act
        using var batch = await messageStore.ReadMessagesAsync(10, "maxdc-queue", consumerId, CancellationToken.None);

        // assert
        Assert.Single(batch.Messages);
        Assert.True(batch.Messages[0].MaxDeliveryCount > 0, "MaxDeliveryCount should be set");
    }

    [Fact]
    public async Task GetNextScheduledTimeAsync_Should_ReturnNull_When_NoScheduledMessages()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var (connectionManager, messageStore) = await CreateStoreAsync(db);
        await CreateQueueAsync(db, "sched-queue");

        // act
        var result = await messageStore.GetNextScheduledTimeAsync("sched-queue", CancellationToken.None);

        // assert
        Assert.Null(result);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task PublishAsync_Should_InsertNothing_When_NoSubscribers()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var (connectionManager, messageStore) = await CreateStoreAsync(db);
        await CreateTopicAsync(db, "lonely-topic");

        // act
        await messageStore.PublishAsync("{}"u8.ToArray(), null, "lonely-topic", CancellationToken.None);

        // assert
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM mocha_message";
        var count = (long)(await cmd.ExecuteScalarAsync())!;
        Assert.Equal(0, count);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task ReadMessagesAsync_Should_SkipLockedMessages_When_ConcurrentConsumers()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var (connectionManager, messageStore) = await CreateStoreAsync(db);
        await CreateQueueAsync(db, "lock-queue");
        var consumer1 = Guid.NewGuid();
        var consumer2 = Guid.NewGuid();
        await RegisterConsumerAsync(db, consumer1);
        await RegisterConsumerAsync(db, consumer2);

        await messageStore.SendAsync("{}"u8.ToArray(), null, "lock-queue", CancellationToken.None);

        // act - consumer 1 reads the message (locks it)
        using var batch1 = await messageStore.ReadMessagesAsync(10, "lock-queue", consumer1, CancellationToken.None);
        // consumer 2 tries to read - should get nothing (message is locked)
        using var batch2 = await messageStore.ReadMessagesAsync(10, "lock-queue", consumer2, CancellationToken.None);

        // assert
        Assert.Single(batch1.Messages);
        Assert.Empty(batch2.Messages);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task ReadMessagesAsync_Should_SetExceededMaxDelivery_When_DeliveryCountExceedsMax()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var (connectionManager, messageStore) = await CreateStoreAsync(db);
        await CreateQueueAsync(db, "exceed-queue");
        var consumerId = Guid.NewGuid();
        await RegisterConsumerAsync(db, consumerId);

        await messageStore.SendAsync("{}"u8.ToArray(), null, "exceed-queue", CancellationToken.None);

        // Set max_delivery_count to 1 so first read exceeds it
        await using (var conn = new NpgsqlConnection(db.ConnectionString))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE mocha_message SET max_delivery_count = 1";
            await cmd.ExecuteNonQueryAsync();
        }

        // act
        using var batch = await messageStore.ReadMessagesAsync(10, "exceed-queue", consumerId, CancellationToken.None);

        // assert
        Assert.Single(batch.Messages);
        Assert.True(batch.Messages[0].ExceededMaxDelivery);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task ReadMessagesAsync_Should_RespectBatchSize_When_MultipleMessagesExist()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var (connectionManager, messageStore) = await CreateStoreAsync(db);
        await CreateQueueAsync(db, "batch-queue");
        var consumerId = Guid.NewGuid();
        await RegisterConsumerAsync(db, consumerId);

        for (var i = 0; i < 5; i++)
        {
            await messageStore.SendAsync("{}"u8.ToArray(), null, "batch-queue", CancellationToken.None);
        }

        // act - read only 2
        using var batch = await messageStore.ReadMessagesAsync(2, "batch-queue", consumerId, CancellationToken.None);

        // assert
        Assert.Equal(2, batch.Messages.Count);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task ReadMessagesAsync_Should_IncrementDeliveryCount_When_MessageRedelivered()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var (connectionManager, messageStore) = await CreateStoreAsync(db);
        await CreateQueueAsync(db, "redeliver-queue");
        var consumerId = Guid.NewGuid();
        await RegisterConsumerAsync(db, consumerId);

        await messageStore.SendAsync("{}"u8.ToArray(), null, "redeliver-queue", CancellationToken.None);

        // first read
        using var batch1 = await messageStore.ReadMessagesAsync(
            10,
            "redeliver-queue",
            consumerId,
            CancellationToken.None);
        Assert.Equal(1, batch1.Messages[0].DeliveryCount);
        var messageId = batch1.Messages[0].TransportMessageId;

        // release for redelivery
        await messageStore.ReleaseMessageAsync(messageId, CancellationToken.None);

        // Clear the backoff delay so the message is immediately eligible
        await using (var conn = new NpgsqlConnection(db.ConnectionString))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE mocha_message SET last_delivered = NULL WHERE transport_message_id = @id";
            cmd.Parameters.AddWithValue("id", messageId);
            await cmd.ExecuteNonQueryAsync();
        }

        // act - second read
        using var batch2 = await messageStore.ReadMessagesAsync(
            10,
            "redeliver-queue",
            consumerId,
            CancellationToken.None);

        // assert
        Assert.Single(batch2.Messages);
        Assert.Equal(2, batch2.Messages[0].DeliveryCount);

        await connectionManager.DisposeAsync();
    }

    [Fact]
    public async Task GetNextScheduledTimeAsync_Should_ReturnTime_When_ScheduledMessageExists()
    {
        // arrange
        await using var db = await _fixture.CreateDatabaseAsync();
        var (connectionManager, messageStore) = await CreateStoreAsync(db);
        await CreateQueueAsync(db, "future-queue");

        await messageStore.SendAsync("{}"u8.ToArray(), null, "future-queue", CancellationToken.None);

        // Set a future scheduled_time
        var scheduledTime = DateTime.UtcNow.AddMinutes(5);
        await using (var conn = new NpgsqlConnection(db.ConnectionString))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE mocha_message SET scheduled_time = @time";
            cmd.Parameters.AddWithValue("time", scheduledTime);
            await cmd.ExecuteNonQueryAsync();
        }

        // act
        var result = await messageStore.GetNextScheduledTimeAsync("future-queue", CancellationToken.None);

        // assert
        Assert.NotNull(result);
        Assert.True(result.Value > DateTimeOffset.UtcNow, "Scheduled time should be in the future");

        await connectionManager.DisposeAsync();
    }

    private async Task<(PostgresConnectionManager, PostgresMessageStore)> CreateStoreAsync(DatabaseContext db)
    {
        var logger = NullLogger<PostgresConnectionManager>.Instance;
        var schemaOptions = new PostgresSchemaOptions();
        var connectionManager = new PostgresConnectionManager(db.ConnectionString, schemaOptions, logger);
        await connectionManager.EnsureMigratedAsync(CancellationToken.None);
        var messageStore = new PostgresMessageStore(connectionManager, schemaOptions);
        return (connectionManager, messageStore);
    }

    private static async Task CreateQueueAsync(DatabaseContext db, string name)
    {
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO mocha_queue (name) VALUES (@name) ON CONFLICT (name) DO NOTHING";
        cmd.Parameters.AddWithValue("name", name);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task CreateTopicAsync(DatabaseContext db, string name)
    {
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO mocha_topic (name) VALUES (@name) ON CONFLICT (name) DO NOTHING";
        cmd.Parameters.AddWithValue("name", name);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task CreateSubscriptionAsync(DatabaseContext db, string topicName, string queueName)
    {
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO mocha_queue_subscription (source_id, destination_id)
            SELECT t.id, q.id
            FROM mocha_topic t, mocha_queue q
            WHERE t.name = @topicName AND q.name = @queueName
            ON CONFLICT DO NOTHING
            """;
        cmd.Parameters.AddWithValue("topicName", topicName);
        cmd.Parameters.AddWithValue("queueName", queueName);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task RegisterConsumerAsync(DatabaseContext db, Guid consumerId)
    {
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO mocha_consumers (id, service_name) VALUES (@id, 'test') ON CONFLICT DO NOTHING";
        cmd.Parameters.AddWithValue("id", consumerId);
        await cmd.ExecuteNonQueryAsync();
    }
}
