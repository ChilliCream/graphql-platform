using Npgsql;

namespace Mocha.Scheduling;

/// <summary>
/// Configuration options for the Postgres scheduled message store, including pre-built SQL queries
/// and the connection factory used by the scheduled message dispatcher.
/// </summary>
internal sealed class PostgresScheduledMessageOptions
{
    /// <summary>
    /// Gets or sets the pre-built SQL queries used for scheduled message insert, poll, process, and delete operations.
    /// </summary>
    public ScheduledMessageQueries Queries { get; set; } = null!;

    /// <summary>
    /// Gets or sets a factory that creates a closed connection using services from the current scope.
    /// </summary>
    public Func<IServiceProvider, NpgsqlConnection> CreateConnection { get; set; } = null!;

    /// <summary>
    /// Gets or sets the number of failed attempts after which a message is reported as failing repeatedly.
    /// The report is repeated each time the attempt count reaches a multiple of this value.
    /// </summary>
    public int CriticalAttemptInterval { get; set; } = 10;

    /// <summary>
    /// Gets or sets how long the dispatcher waits before it looks again for a due message that another
    /// dispatcher holds.
    /// </summary>
    public TimeSpan HeldMessagePollingInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets how long the dispatcher pauses after a failed dispatch. The default is the retry delay
    /// of a message.
    /// </summary>
    public TimeSpan FailurePause { get; set; } = TimeSpan.FromSeconds(ScheduledMessageQueries.RetryDelaySeconds);
}
