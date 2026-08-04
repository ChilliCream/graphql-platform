namespace Mocha.Outbox;

/// <summary>
/// Configuration options for the Postgres message outbox, including pre-built SQL queries
/// and the connection string used by the outbox worker.
/// </summary>
internal sealed class PostgresMessageOutboxOptions
{
    /// <summary>
    /// Gets or sets the pre-built SQL queries used for outbox insert, poll, process, and delete operations.
    /// </summary>
    public PostgresMessageOutboxQueries Queries { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Postgres connection string used by the outbox worker to open a dedicated connection.
    /// </summary>
    public string ConnectionString { get; set; } = null!;
}
