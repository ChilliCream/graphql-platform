namespace Mocha.EntityFrameworkCore.SqlServer;

/// <summary>
/// Contains table and column information for SQL Server messaging tables.
/// Populated from EF Core model metadata during configuration.
/// </summary>
public sealed class SqlServerTableInfo
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
