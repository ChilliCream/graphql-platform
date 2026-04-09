namespace Mocha.EntityFrameworkCore.SqlServer;

/// <summary>
/// Table and column information for the saga states table.
/// </summary>
public sealed class SagaStateTableInfo
{
    /// <summary>
    /// Gets or sets the database schema for the saga states table. Defaults to <c>"dbo"</c>.
    /// </summary>
    public string Schema { get; set; } = "dbo";

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
    /// Gets the fully qualified table name including schema if not dbo.
    /// </summary>
    public string QualifiedTableName => $"[{Schema}].[{Table}]";
}
