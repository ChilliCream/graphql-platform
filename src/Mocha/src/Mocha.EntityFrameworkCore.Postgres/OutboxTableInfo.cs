namespace Mocha.EntityFrameworkCore.Postgres;

/// <summary>
/// Table and column information for the outbox messages table.
/// </summary>
public sealed class OutboxTableInfo
{
    /// <summary>
    /// Gets or sets the database schema for the outbox table. Defaults to <c>"public"</c>.
    /// </summary>
    public string Schema { get; set; } = "public";

    /// <summary>
    /// Gets or sets the table name for outbox messages. Defaults to <c>"outbox_messages"</c>.
    /// </summary>
    public string Table { get; set; } = "outbox_messages";

    /// <summary>
    /// Gets or sets the column name for the outbox message identifier. Defaults to <c>"id"</c>.
    /// </summary>
    public string Id { get; set; } = "id";

    /// <summary>
    /// Gets or sets the column name for the serialized message envelope. Defaults to <c>"envelope"</c>.
    /// </summary>
    public string Envelope { get; set; } = "envelope";

    /// <summary>
    /// Gets or sets the column name tracking how many times the message has been dispatched. Defaults to <c>"times_sent"</c>.
    /// </summary>
    public string TimesSent { get; set; } = "times_sent";

    /// <summary>
    /// Gets or sets the column name for the message creation timestamp. Defaults to <c>"created_at"</c>.
    /// </summary>
    public string CreatedAt { get; set; } = "created_at";

    /// <summary>
    /// Gets or sets the name of the primary key index. Defaults to <c>"ix_outbox_messages_primary_key"</c>.
    /// </summary>
    public string IxPrimaryKey { get; set; } = "ix_outbox_messages_primary_key";

    /// <summary>
    /// Gets or sets the name of the created-at index used for ordering outbox processing. Defaults to <c>"ix_outbox_messages_created_at"</c>.
    /// </summary>
    public string IxCreatedAt { get; set; } = "ix_outbox_messages_created_at";

    /// <summary>
    /// Gets or sets the name of the times-sent index used for retry filtering. Defaults to <c>"ix_outbox_messages_times_sent"</c>.
    /// </summary>
    public string IxTimesSent { get; set; } = "ix_outbox_messages_times_sent";

    /// <summary>
    /// Gets the fully qualified table name including schema if not public.
    /// </summary>
    public string QualifiedTableName
        => string.IsNullOrEmpty(Schema) || Schema == "public" ? $"\"{Table}\"" : $"\"{Schema}\".\"{Table}\"";
}
