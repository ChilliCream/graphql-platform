namespace Mocha.EntityFrameworkCore.SqlServer;

/// <summary>
/// Table and column information for the inbox messages table.
/// </summary>
public sealed class InboxTableInfo
{
    /// <summary>
    /// Gets or sets the database schema for the inbox table. Defaults to <c>"dbo"</c>.
    /// </summary>
    public string Schema { get; set; } = "dbo";

    /// <summary>
    /// Gets or sets the table name for inbox messages. Defaults to <c>"inbox_messages"</c>.
    /// </summary>
    public string Table { get; set; } = "inbox_messages";

    /// <summary>
    /// Gets or sets the column name for the message identifier. Defaults to <c>"message_id"</c>.
    /// </summary>
    public string MessageId { get; set; } = "message_id";

    /// <summary>
    /// Gets or sets the column name for the consumer type. Defaults to <c>"consumer_type"</c>.
    /// </summary>
    public string ConsumerType { get; set; } = "consumer_type";

    /// <summary>
    /// Gets or sets the column name for the message type. Defaults to <c>"message_type"</c>.
    /// </summary>
    public string MessageType { get; set; } = "message_type";

    /// <summary>
    /// Gets or sets the column name for the processed-at timestamp. Defaults to <c>"processed_at"</c>.
    /// </summary>
    public string ProcessedAt { get; set; } = "processed_at";

    /// <summary>
    /// Gets or sets the name of the primary key index. Defaults to <c>"ix_inbox_messages_primary_key"</c>.
    /// </summary>
    public string IxPrimaryKey { get; set; } = "ix_inbox_messages_primary_key";

    /// <summary>
    /// Gets or sets the name of the processed-at index used for cleanup ordering. Defaults to <c>"ix_inbox_messages_processed_at"</c>.
    /// </summary>
    public string IxProcessedAt { get; set; } = "ix_inbox_messages_processed_at";

    /// <summary>
    /// Gets the fully qualified table name including schema if not dbo.
    /// </summary>
    public string QualifiedTableName => $"[{Schema}].[{Table}]";
}
