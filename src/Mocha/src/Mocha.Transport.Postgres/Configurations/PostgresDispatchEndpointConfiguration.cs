namespace Mocha.Transport.Postgres;

/// <summary>
/// Configuration for a PostgreSQL dispatch endpoint.
/// </summary>
public sealed class PostgresDispatchEndpointConfiguration : DispatchEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the target queue name for direct dispatch.
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Gets or sets the target topic name for publish dispatch.
    /// </summary>
    public string? TopicName { get; set; }
}
