using Mocha.EntityFrameworkCore.SqlServer;

namespace Mocha.Inbox;

/// <summary>
/// Holds pre-built SQL query strings for SQL Server inbox operations, generated from
/// <see cref="InboxTableInfo"/> column and table metadata.
/// </summary>
internal sealed class SqlServerMessageInboxQueries
{
    /// <summary>
    /// Gets or sets the SQL query to check if a message has already been processed by a specific consumer type.
    /// </summary>
    public string Exists { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL statement to insert a processed message record into the inbox table.
    /// Uses a NOT EXISTS guard for idempotent inserts.
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
    /// Creates a new <see cref="SqlServerMessageInboxQueries"/> instance with SQL queries built from the provided table metadata.
    /// </summary>
    /// <param name="tableInfo">The inbox table info containing column and table names.</param>
    /// <returns>A fully initialized <see cref="SqlServerMessageInboxQueries"/> instance.</returns>
    public static SqlServerMessageInboxQueries From(InboxTableInfo tableInfo)
    {
        var table = tableInfo.QualifiedTableName;

        return new SqlServerMessageInboxQueries
        {
            Exists =
                $"""
                 SELECT CASE WHEN EXISTS(
                     SELECT 1 FROM {table}
                     WHERE [{tableInfo.MessageId}] = @message_id AND [{tableInfo.ConsumerType}] = @consumer_type
                 ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;
                 """,
            Insert =
                $"""
                 INSERT INTO {table} ([{tableInfo.MessageId}], [{tableInfo.ConsumerType}], [{tableInfo.MessageType}], [{tableInfo.ProcessedAt}])
                 SELECT @message_id, @consumer_type, @message_type, SYSUTCDATETIME()
                 WHERE NOT EXISTS (
                     SELECT 1 FROM {table} WITH (UPDLOCK, HOLDLOCK)
                     WHERE [{tableInfo.MessageId}] = @message_id AND [{tableInfo.ConsumerType}] = @consumer_type
                 );
                 """,
            TryClaim =
                $"""
                 INSERT INTO {table} ([{tableInfo.MessageId}], [{tableInfo.ConsumerType}], [{tableInfo.MessageType}], [{tableInfo.ProcessedAt}])
                 SELECT @message_id, @consumer_type, @message_type, SYSUTCDATETIME()
                 WHERE NOT EXISTS (
                     SELECT 1 FROM {table} WITH (UPDLOCK, HOLDLOCK)
                     WHERE [{tableInfo.MessageId}] = @message_id AND [{tableInfo.ConsumerType}] = @consumer_type
                 );
                 """,
            Cleanup =
                $"""
                 DELETE TOP(1000) FROM {table} WHERE [{tableInfo.ProcessedAt}] < @cutoff;
                 """
        };
    }
}
