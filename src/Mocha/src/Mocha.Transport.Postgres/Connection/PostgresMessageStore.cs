using System.Data;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

namespace Mocha.Transport.Postgres;

/// <summary>
/// Provides database operations for storing and retrieving messages from PostgreSQL.
/// Handles message dispatch (publish/send) and receive (read/delete/release) operations.
/// </summary>
public sealed class PostgresMessageStore
{
    private readonly PostgresConnectionManager _connectionManager;
    private readonly IReadOnlyPostgresSchemaOptions _schemaOptions;

    public PostgresMessageStore(
        PostgresConnectionManager connectionManager,
        IReadOnlyPostgresSchemaOptions schemaOptions)
    {
        _connectionManager = connectionManager;
        _schemaOptions = schemaOptions;
    }

    /// <summary>
    /// Publishes a message to all queues subscribed to the specified topic.
    /// Uses a single INSERT...SELECT to fan out the message and NOTIFY to signal receivers.
    /// </summary>
    public async Task PublishAsync(
        ReadOnlyMemory<byte> body,
        ReadOnlyMemory<byte> headers,
        string topicName,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionManager.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = $"""
            WITH inserted_messages AS (
                INSERT INTO {_schemaOptions.MessageTable} (body, headers, queue_id)
                SELECT @body, @headers, qs.destination_id
                FROM {_schemaOptions.QueueSubscriptionTable} qs
                INNER JOIN {_schemaOptions.TopicTable} t ON qs.source_id = t.id
                WHERE t.name = @topic_name
                RETURNING queue_id
            )
            SELECT pg_notify(
                '{_schemaOptions.NotificationChannel}',
                q.name::text
            )
            FROM inserted_messages
            JOIN {_schemaOptions.QueueTable} q ON inserted_messages.queue_id = q.id;
            """;

        command.Parameters.Add(new NpgsqlParameter("body", NpgsqlDbType.Bytea) { Value = body });
        command.Parameters.Add(
            new NpgsqlParameter("headers", NpgsqlDbType.Jsonb) { Value = !headers.IsEmpty ? headers : DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter("topic_name", NpgsqlDbType.Text) { Value = topicName });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Sends a message directly to a specific queue.
    /// </summary>
    public async Task SendAsync(
        ReadOnlyMemory<byte> body,
        ReadOnlyMemory<byte> headers,
        string queueName,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionManager.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = $"""
            WITH queue_info AS (
                SELECT id FROM {_schemaOptions.QueueTable} WHERE name = @queue_name LIMIT 1
            ),
            inserted_message AS (
                INSERT INTO {_schemaOptions.MessageTable} (body, headers, queue_id)
                SELECT @body, @headers, queue_info.id
                FROM queue_info
                RETURNING queue_id
            )
            SELECT pg_notify(
                '{_schemaOptions.NotificationChannel}',
                q.name::text
            )
            FROM inserted_message
            JOIN {_schemaOptions.QueueTable} q ON inserted_message.queue_id = q.id;
            """;

        command.Parameters.Add(new NpgsqlParameter("body", NpgsqlDbType.Bytea) { Value = body });
        command.Parameters.Add(
            new NpgsqlParameter("headers", NpgsqlDbType.Jsonb) { Value = !headers.IsEmpty ? headers : DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter("queue_name", NpgsqlDbType.Text) { Value = queueName });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Reads and locks a batch of messages from the specified queue for processing.
    /// Uses SELECT ... FOR UPDATE SKIP LOCKED to support concurrent consumers.
    /// Messages that have exceeded their max delivery count are also returned with
    /// the <see cref="PostgresMessageItem.ExceededMaxDelivery"/> flag set.
    /// </summary>
    public async Task<PostgresMessageBatch> ReadMessagesAsync(
        int count,
        string queueName,
        Guid consumerId,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionManager.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = $"""
            WITH eligible_messages AS (
                SELECT m.*
                FROM {_schemaOptions.MessageTable} m
                INNER JOIN {_schemaOptions.QueueTable} q ON m.queue_id = q.id
                WHERE q.name = @queue_name
                  AND m.consumer_id IS NULL
                  AND (m.expiration_time IS NULL OR m.expiration_time > now() AT TIME ZONE 'utc')
                  AND (m.scheduled_time IS NULL OR m.scheduled_time <= now() AT TIME ZONE 'utc')
                  AND (m.last_delivered IS NULL OR m.last_delivered + (INTERVAL '1 second' * power(2, LEAST(m.delivery_count, 10))) <= now() AT TIME ZONE 'utc')
                FOR UPDATE SKIP LOCKED
                LIMIT @count
            ),
            updated_messages AS (
                UPDATE {_schemaOptions.MessageTable}
                SET consumer_id = @consumer_id,
                    last_delivered = now() AT TIME ZONE 'utc',
                    delivery_count = {_schemaOptions.MessageTable}.delivery_count + 1
                FROM eligible_messages em
                WHERE {_schemaOptions.MessageTable}.transport_message_id = em.transport_message_id
                RETURNING
                    {_schemaOptions.MessageTable}.transport_message_id,
                    {_schemaOptions.MessageTable}.body,
                    {_schemaOptions.MessageTable}.headers,
                    {_schemaOptions.MessageTable}.queue_id,
                    {_schemaOptions.MessageTable}.sent_time,
                    {_schemaOptions.MessageTable}.delivery_count,
                    {_schemaOptions.MessageTable}.max_delivery_count,
                    {_schemaOptions.MessageTable}.error_reason
            )
            SELECT
                um.transport_message_id,
                um.body,
                um.headers,
                um.queue_id,
                um.sent_time,
                um.delivery_count,
                um.max_delivery_count,
                um.error_reason
            FROM updated_messages um
            """;

        command.Parameters.Add(new NpgsqlParameter("queue_name", NpgsqlDbType.Text) { Value = queueName });
        command.Parameters.Add(new NpgsqlParameter("consumer_id", NpgsqlDbType.Uuid) { Value = consumerId });
        command.Parameters.Add(new NpgsqlParameter("count", NpgsqlDbType.Integer) { Value = count });

        var batch = new PostgresMessageBatch();
        await using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var transportMessageId = reader.GetGuid(0);
                var body = await ReadBytesAsync(batch, reader, 1, cancellationToken);
                var headers = await ReadBytesAsync(batch, reader, 2, cancellationToken);
                var queueId = reader.GetInt64(3);
                var sentTime = reader.GetDateTime(4);
                var deliveryCount = reader.GetInt32(5);
                var maxDeliveryCount = reader.GetInt32(6);
                var errorReason = reader.IsDBNull(7) ? null : reader.GetString(7);

                batch.Messages.Add(
                    new PostgresMessageItem(
                        transportMessageId,
                        body,
                        headers,
                        queueId,
                        sentTime,
                        deliveryCount,
                        maxDeliveryCount,
                        errorReason,
                        ExceededMaxDelivery: deliveryCount >= maxDeliveryCount));
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return batch;
    }

    private static async Task<ReadOnlyMemory<byte>> ReadBytesAsync(
        PostgresMessageBatch batch,
        NpgsqlDataReader reader,
        int ordinal,
        CancellationToken cancellationToken)
    {
        if (reader.IsDBNull(ordinal))
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        var bytes = await reader.GetFieldValueAsync<ReadOnlyMemory<byte>>(ordinal, cancellationToken);

        if (bytes.Length == 0)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        var memory = batch.GetMemory(bytes.Length);
        bytes.CopyTo(memory);
        return memory;
    }

    /// <summary>
    /// Deletes a message after successful processing.
    /// </summary>
    public async Task DeleteMessageAsync(Guid transportMessageId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionManager.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = $"DELETE FROM {_schemaOptions.MessageTable} WHERE transport_message_id = @id";
        command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = transportMessageId });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Releases a message back to the queue for redelivery after a processing failure.
    /// Optionally records error information from the failed attempt.
    /// </summary>
    public async Task ReleaseMessageAsync(
        Guid transportMessageId,
        ErrorInfo? errorInfo,
        CancellationToken cancellationToken)
    {
        if (errorInfo is not null)
        {
            await UpdateErrorReasonAsync(transportMessageId, errorInfo, cancellationToken);
        }

        await using var connection = await _connectionManager.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = $"""
            WITH updated_message AS (
                UPDATE {_schemaOptions.MessageTable}
                SET consumer_id = NULL
                WHERE transport_message_id = @id
                RETURNING queue_id
            )
            SELECT pg_notify(
                '{_schemaOptions.NotificationChannel}',
                q.name::text
            )
            FROM updated_message
            JOIN {_schemaOptions.QueueTable} q ON updated_message.queue_id = q.id;
            """;

        command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = transportMessageId });
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Releases a message back to the queue for redelivery after a processing failure.
    /// </summary>
    public Task ReleaseMessageAsync(Guid transportMessageId, CancellationToken cancellationToken)
        => ReleaseMessageAsync(transportMessageId, errorInfo: null, cancellationToken);

    /// <summary>
    /// Appends error information to the <c>error_reason</c> JSONB array on a message.
    /// Called when a message processing attempt fails but has not yet exceeded the retry limit.
    /// </summary>
    public async Task UpdateErrorReasonAsync(
        Guid transportMessageId,
        ErrorInfo errorInfo,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionManager.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = $"""
            UPDATE {_schemaOptions.MessageTable}
            SET error_reason = COALESCE(error_reason, '[]'::jsonb) || @error_entry::jsonb
            WHERE transport_message_id = @id
            """;

        var errorEntry = JsonSerializer.Serialize(new[] { errorInfo });

        command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = transportMessageId });
        command.Parameters.Add(new NpgsqlParameter("error_entry", NpgsqlDbType.Jsonb) { Value = errorEntry });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Queries for the next scheduled message time in a queue, considering both scheduled_time
    /// and retry backoff delays. Used by the delayed trigger to avoid busy-waiting.
    /// </summary>
    public async Task<DateTimeOffset?> GetNextScheduledTimeAsync(string queueName, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionManager.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = $"""
            SELECT min(
                LEAST(
                    m.scheduled_time,
                    m.last_delivered + (INTERVAL '1 second' * power(2, LEAST(m.delivery_count, 10)))
                )
            )
            FROM {_schemaOptions.MessageTable} m
            INNER JOIN {_schemaOptions.QueueTable} q ON m.queue_id = q.id
            WHERE q.name = @queue_name
              AND m.consumer_id IS NULL
              AND m.delivery_count < m.max_delivery_count
              AND (m.expiration_time IS NULL OR m.expiration_time > now() AT TIME ZONE 'utc')
              AND (
                  m.scheduled_time IS NOT NULL OR
                  (m.last_delivered IS NOT NULL AND m.last_delivered + (INTERVAL '1 second' * power(2, LEAST(m.delivery_count, 10))) > now() AT TIME ZONE 'utc')
              );
            """;

        command.Parameters.Add(new NpgsqlParameter("queue_name", NpgsqlDbType.Text) { Value = queueName });

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is DateTime dt ? new DateTimeOffset(dt, TimeSpan.Zero) : null;
    }
}

/// <summary>
/// Represents a message item read from the PostgreSQL message store.
/// </summary>
public sealed record PostgresMessageItem(
    Guid TransportMessageId,
    ReadOnlyMemory<byte> Body,
    ReadOnlyMemory<byte> Headers,
    long QueueId,
    DateTime SentTime,
    int DeliveryCount,
    int MaxDeliveryCount,
    string? ErrorReason,
    bool ExceededMaxDelivery = false);
