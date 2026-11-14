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
    /// Gets or sets the maximum number of messages dispatched concurrently.
    /// </summary>
    public int MaxConcurrentMessages { get; set; } = 1;

    /// <summary>
    /// Gets or sets the longest time a single dispatch may take, or <see cref="Timeout.InfiniteTimeSpan"/>
    /// for no limit.
    /// </summary>
    public TimeSpan DispatchTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the number of failed dispatch attempts after which a message is no longer retried.
    /// </summary>
    public int MaxAttempts { get; set; } = 10;

    /// <summary>
    /// Gets or sets the longest time an idle worker waits before it reads the outbox again.
    /// </summary>
    public TimeSpan IdlePollingInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the time a worker waits before it looks again for a due message that another worker holds.
    /// </summary>
    public TimeSpan HeldMessagePollingInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the first pause of a worker after a released message or a failure of the worker.
    /// The pause doubles with every further one up to <see cref="MaxPauseDelay"/>.
    /// </summary>
    public TimeSpan PauseDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets the longest pause of a worker after a released message or a failure of the worker.
    /// </summary>
    public TimeSpan MaxPauseDelay { get; set; } = TimeSpan.FromSeconds(10);
}
