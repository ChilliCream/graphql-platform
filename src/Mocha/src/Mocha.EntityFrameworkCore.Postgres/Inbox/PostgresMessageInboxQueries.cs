using Mocha.EntityFrameworkCore.Postgres;

namespace Mocha.Inbox;

/// <summary>
/// Holds pre-built SQL query strings for Postgres inbox operations, generated from
/// <see cref="InboxTableInfo"/> column and table metadata.
/// </summary>
internal sealed class PostgresMessageInboxQueries
{
    /// <summary>
    /// Gets or sets the SQL query to check if a message has already been processed by a specific consumer type.
    /// </summary>
    public string Exists { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL statement to insert a processed message record into the inbox table.
    /// Uses ON CONFLICT DO NOTHING for idempotent inserts.
    /// </summary>
    public string Insert { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL statement to atomically attempt to claim a message by inserting it.
    /// Returns the number of affected rows (1 if claimed, 0 if already claimed).
    /// </summary>
    public string TryClaim { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL statement to delete old processed inbox messages.
    /// </summary>
    public string Cleanup { get; set; } = null!;

    /// <summary>
    /// Creates a new <see cref="PostgresMessageInboxQueries"/> instance with SQL queries built from the provided table metadata.
    /// </summary>
    /// <param name="table">The inbox table info containing column and table names.</param>
    /// <returns>A fully initialized <see cref="PostgresMessageInboxQueries"/> instance.</returns>
    public static PostgresMessageInboxQueries From(InboxTableInfo table)
    {
        return new PostgresMessageInboxQueries
        {
            Exists =
                $"""
                 SELECT EXISTS(
                     SELECT 1 FROM {table.QualifiedTableName}
                     WHERE "{table.MessageId}" = @message_id AND "{table.ConsumerType}" = @consumer_type
                 )
                 """,
            Insert =
                $"""
                 INSERT INTO {table.QualifiedTableName} ("{table.MessageId}", "{table.ConsumerType}", "{table.MessageType}", "{table.ProcessedAt}")
                 VALUES (@message_id, @consumer_type, @message_type, NOW())
                 ON CONFLICT ("{table.MessageId}", "{table.ConsumerType}") DO NOTHING
                 """,
            TryClaim =
                $"""
                 INSERT INTO {table.QualifiedTableName} ("{table.MessageId}", "{table.ConsumerType}", "{table.MessageType}", "{table.ProcessedAt}")
                 VALUES (@message_id, @consumer_type, @message_type, NOW())
                 ON CONFLICT ("{table.MessageId}", "{table.ConsumerType}") DO NOTHING
                 """,
            Cleanup =
                $"""
                 DELETE FROM {table.QualifiedTableName}
                 WHERE ctid IN (
                     SELECT ctid FROM {table.QualifiedTableName}
                     WHERE "{table.ProcessedAt}" < @cutoff
                     LIMIT 1000
                 )
                 """
        };
    }
}
