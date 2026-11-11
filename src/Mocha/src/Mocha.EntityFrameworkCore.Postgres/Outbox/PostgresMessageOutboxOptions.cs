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
}
