using Npgsql;

namespace Mocha.Outbox;

/// <summary>
/// Configuration options for the Postgres message outbox, including pre-built SQL queries
/// and the connection factory used by the outbox worker.
/// </summary>
internal sealed class PostgresMessageOutboxOptions
{
    /// <summary>
    /// Gets or sets the pre-built SQL queries used for outbox insert, poll, process, and delete operations.
    /// </summary>
    public PostgresMessageOutboxQueries Queries { get; set; } = null!;

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
    /// Gets or sets how long an idle processor waits before checking for messages when no notification arrives.
    /// </summary>
    public TimeSpan PollingInterval { get; set; }

    /// <summary>
    /// Gets or sets how long the processor waits before it looks again for a due message that another
    /// instance holds.
    /// </summary>
    public TimeSpan HeldMessagePollingInterval { get; set; } = TimeSpan.FromSeconds(1);
}
