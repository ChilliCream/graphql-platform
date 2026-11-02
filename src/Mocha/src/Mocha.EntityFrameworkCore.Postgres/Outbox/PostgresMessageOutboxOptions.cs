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
}
