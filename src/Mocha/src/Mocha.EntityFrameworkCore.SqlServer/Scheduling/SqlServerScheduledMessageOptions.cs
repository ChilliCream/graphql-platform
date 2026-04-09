namespace Mocha.Scheduling;

/// <summary>
/// Configuration options for the SQL Server scheduled message store, including pre-built T-SQL queries
/// and the connection string used by the scheduled message dispatcher.
/// </summary>
internal sealed class SqlServerScheduledMessageOptions
{
    /// <summary>
    /// Gets or sets the pre-built T-SQL queries used for scheduled message insert, poll, process, and delete operations.
    /// </summary>
    public SqlServerScheduledMessageQueries Queries { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SQL Server connection string used by the dispatcher to open a dedicated connection.
    /// </summary>
    public string ConnectionString { get; set; } = null!;
}
