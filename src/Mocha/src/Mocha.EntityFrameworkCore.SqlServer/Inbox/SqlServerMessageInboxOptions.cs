namespace Mocha.Inbox;

/// <summary>
/// Configuration options for the SQL Server message inbox, including pre-built SQL queries
/// and the connection string used by the inbox cleanup worker.
/// </summary>
internal sealed class SqlServerMessageInboxOptions
{
    /// <summary>
    /// Gets or sets the pre-built SQL queries used for inbox exists, insert, and cleanup operations.
    /// </summary>
    public SqlServerMessageInboxQueries Queries { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL Server connection string used by the inbox cleanup worker.
    /// </summary>
    public string ConnectionString { get; set; } = null!;
}
