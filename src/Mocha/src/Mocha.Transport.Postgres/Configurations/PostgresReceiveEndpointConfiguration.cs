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

    /// <summary>
    /// Gets or sets the configuration for the error queue satellite that handles failed messages.
    /// </summary>
    public PostgresSatelliteConfiguration ErrorQueue { get; set; } = new();

    /// <summary>
    /// Gets or sets the configuration for the skipped queue satellite that handles skipped messages.
    /// </summary>
    public PostgresSatelliteConfiguration SkippedQueue { get; set; } = new();

    public static new class Defaults
    {
        /// <summary>
        /// The default maximum batch size, set to 10.
        /// </summary>
        public static int MaxBatchSize = 10;
    }
}
