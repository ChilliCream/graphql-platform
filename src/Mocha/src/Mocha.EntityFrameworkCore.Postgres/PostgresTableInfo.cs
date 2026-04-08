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

    /// <summary>
    /// Gets or sets the table and column metadata for the inbox messages table.
    /// </summary>
    public InboxTableInfo Inbox { get; set; } = new();

    /// <summary>
    /// Gets or sets the table and column metadata for the scheduled messages table.
    /// </summary>
    public ScheduledMessageTableInfo ScheduledMessage { get; set; } = new();
}
