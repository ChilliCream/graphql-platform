namespace Mocha.Outbox;

/// <summary>
/// Configuration options for the SQL Server message outbox, including pre-built SQL queries
/// and the connection string used by the outbox worker.
/// </summary>
internal sealed class SqlServerMessageOutboxOptions
{
    /// <summary>
    /// Gets or sets the pre-built SQL queries used for outbox insert, poll, process, and delete operations.
    /// </summary>
    public SqlServerMessageOutboxQueries Queries { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL Server connection string used by the outbox worker to open a dedicated connection.
    /// </summary>
    public string ConnectionString { get; set; } = null!;
}
