namespace Mocha.Transport.Postgres;

/// <summary>
/// Configuration for a PostgreSQL receive endpoint.
/// </summary>
public sealed class PostgresReceiveEndpointConfiguration : ReceiveEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the source queue name.
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of messages to fetch per batch. Defaults to 10.
    /// </summary>
    public int? MaxBatchSize { get; set; }

    public static new class Defaults
    {
        /// <summary>
        /// The default maximum batch size, set to 10.
        /// </summary>
        public static int MaxBatchSize = 10;
    }
}
