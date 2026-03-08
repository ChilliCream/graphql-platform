namespace Mocha.EntityFrameworkCore.Postgres;

/// <summary>
/// Contains table and column information for Postgres messaging tables.
/// Populated from EF Core model metadata during configuration.
/// </summary>
public sealed class PostgresTableInfo
{
    /// <summary>
    /// Gets or sets the table and column metadata for the outbox messages table.
    /// </summary>
    public OutboxTableInfo Outbox { get; set; } = new();

    /// <summary>
    /// Gets or sets the table and column metadata for the saga states table.
    /// </summary>
    public SagaStateTableInfo SagaState { get; set; } = new();
}

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

/// <summary>
/// Table and column information for the saga states table.
/// </summary>
public sealed class SagaStateTableInfo
{
    /// <summary>
    /// Gets or sets the database schema for the saga states table. Defaults to <c>"public"</c>.
    /// </summary>
    public string Schema { get; set; } = "public";

    /// <summary>
    /// Gets or sets the table name for saga states. Defaults to <c>"saga_states"</c>.
    /// </summary>
    public string Table { get; set; } = "saga_states";

    /// <summary>
    /// Gets or sets the column name for the saga instance identifier. Defaults to <c>"id"</c>.
    /// </summary>
    public string Id { get; set; } = "id";

    /// <summary>
    /// Gets or sets the column name for the optimistic concurrency version token. Defaults to <c>"version"</c>.
    /// </summary>
    public string Version { get; set; } = "version";

    /// <summary>
    /// Gets or sets the column name for the logical saga type name. Defaults to <c>"saga_name"</c>.
    /// </summary>
    public string SagaName { get; set; } = "saga_name";

    /// <summary>
    /// Gets or sets the column name for the serialized saga state JSON. Defaults to <c>"state"</c>.
    /// </summary>
    public string State { get; set; } = "state";

    /// <summary>
    /// Gets or sets the column name for the creation timestamp. Defaults to <c>"created_at"</c>.
    /// </summary>
    public string CreatedAt { get; set; } = "created_at";

    /// <summary>
    /// Gets or sets the column name for the last-updated timestamp. Defaults to <c>"updated_at"</c>.
    /// </summary>
    public string UpdatedAt { get; set; } = "updated_at";

    /// <summary>
    /// Gets or sets the name of the composite primary key index on id and saga name. Defaults to <c>"ix_saga_states_primary_key"</c>.
    /// </summary>
    public string IxPrimaryKey { get; set; } = "ix_saga_states_primary_key";

    /// <summary>
    /// Gets or sets the name of the created-at index. Defaults to <c>"ix_saga_states_created_at"</c>.
    /// </summary>
    public string IxCreatedAt { get; set; } = "ix_saga_states_created_at";

    /// <summary>
    /// Gets the fully qualified table name including schema if not public.
    /// </summary>
    public string QualifiedTableName
        => string.IsNullOrEmpty(Schema) || Schema == "public" ? $"\"{Table}\"" : $"\"{Schema}\".\"{Table}\"";
}
