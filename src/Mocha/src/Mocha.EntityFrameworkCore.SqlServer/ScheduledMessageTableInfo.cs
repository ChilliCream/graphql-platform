namespace Mocha.EntityFrameworkCore.SqlServer;

/// <summary>
/// Table and column information for the scheduled messages table.
/// </summary>
public sealed class ScheduledMessageTableInfo
{
    /// <summary>
    /// Gets or sets the database schema for the scheduled messages table. Defaults to <c>"dbo"</c>.
    /// </summary>
    public string Schema { get; set; } = "dbo";

    /// <summary>
    /// Gets or sets the table name for scheduled messages. Defaults to <c>"scheduled_messages"</c>.
    /// </summary>
    public string Table { get; set; } = "scheduled_messages";

    /// <summary>
    /// Gets or sets the column name for the scheduled message identifier. Defaults to <c>"id"</c>.
    /// </summary>
    public string Id { get; set; } = "id";

    /// <summary>
    /// Gets or sets the column name for the serialized message envelope. Defaults to <c>"envelope"</c>.
    /// </summary>
    public string Envelope { get; set; } = "envelope";

    /// <summary>
    /// Gets or sets the column name for the scheduled delivery time. Defaults to <c>"scheduled_time"</c>.
    /// </summary>
    public string ScheduledTime { get; set; } = "scheduled_time";

    /// <summary>
    /// Gets or sets the column name tracking how many times dispatch has been attempted. Defaults
    /// to <c>"times_sent"</c>.
    /// </summary>
    public string TimesSent { get; set; } = "times_sent";

    /// <summary>
    /// Gets or sets the column name for the message creation timestamp. Defaults to <c>"created_at"</c>.
    /// </summary>
    public string CreatedAt { get; set; } = "created_at";

    /// <summary>
    /// Gets or sets the column name for the maximum number of dispatch attempts. Defaults to <c>"max_attempts"</c>.
    /// </summary>
    public string MaxAttempts { get; set; } = "max_attempts";

    /// <summary>
    /// Gets or sets the column name for the last error encountered during dispatch. Defaults to <c>"last_error"</c>.
    /// </summary>
    public string LastError { get; set; } = "last_error";

    /// <summary>
    /// Gets or sets the name of the primary key index. Defaults to <c>"ix_scheduled_messages_primary_key"</c>.
    /// </summary>
    public string IxPrimaryKey { get; set; } = "ix_scheduled_messages_primary_key";

    /// <summary>
    /// Gets or sets the name of the scheduled-time index used for dispatch ordering. Defaults to
    /// <c>"ix_scheduled_messages_scheduled_time"</c>.
    /// </summary>
    public string IxScheduledTime { get; set; } = "ix_scheduled_messages_scheduled_time";

    /// <summary>
    /// Gets or sets the name of the times-sent index used for retry filtering. Defaults to
    /// <c>"ix_scheduled_messages_times_sent"</c>.
    /// </summary>
    public string IxTimesSent { get; set; } = "ix_scheduled_messages_times_sent";

    /// <summary>
    /// Gets the fully qualified table name including schema if not dbo.
    /// </summary>
    public string QualifiedTableName => $"[{Schema}].[{Table}]";
}
