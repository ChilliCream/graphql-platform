namespace Mocha.Scheduling;

/// <summary>
/// Configuration options for the Postgres scheduled message store, including pre-built SQL queries
/// and the connection string used by the scheduled message dispatcher.
/// </summary>
internal sealed class PostgresScheduledMessageOptions
{
    /// <summary>
    /// Gets or sets the pre-built SQL queries used for scheduled message insert, poll, process, and delete operations.
    /// </summary>
    public ScheduledMessageQueries Queries { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Postgres connection string used by the dispatcher to open a dedicated connection.
    /// </summary>
    public string ConnectionString { get; set; } = null!;
}
