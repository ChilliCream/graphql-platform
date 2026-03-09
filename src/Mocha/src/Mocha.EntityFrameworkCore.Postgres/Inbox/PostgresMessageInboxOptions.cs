namespace Mocha.Inbox;

/// <summary>
/// Configuration options for the Postgres message inbox, including pre-built SQL queries
/// and the connection string used by the inbox cleanup worker.
/// </summary>
internal sealed class PostgresMessageInboxOptions
{
    /// <summary>
    /// Gets or sets the pre-built SQL queries used for inbox exists, insert, and cleanup operations.
    /// </summary>
    public PostgresMessageInboxQueries Queries { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Postgres connection string used by the inbox cleanup worker.
    /// </summary>
    public string ConnectionString { get; set; } = null!;
}
