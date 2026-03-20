namespace Mocha.Transport.Postgres;

/// <summary>
/// Default options for receive endpoints created by topology conventions.
/// </summary>
public sealed class PostgresDefaultEndpointOptions
{
    /// <summary>
    /// Gets or sets the default maximum number of messages to fetch per batch.
    /// Default is null (uses the endpoint default of 10).
    /// </summary>
    public int? MaxBatchSize { get; set; }

    /// <summary>
    /// Gets or sets the default maximum number of messages to process concurrently.
    /// Default is null (uses the endpoint default of <see cref="Environment.ProcessorCount"/>).
    /// </summary>
    public int? MaxConcurrency { get; set; }

    /// <summary>
    /// Applies these defaults to a receive endpoint configuration, without overriding explicitly set values.
    /// </summary>
    internal void ApplyTo(PostgresReceiveEndpointConfiguration configuration)
    {
        if (MaxBatchSize is not null && configuration.MaxBatchSize is null)
        {
            configuration.MaxBatchSize = MaxBatchSize.Value;
        }

        if (MaxConcurrency is not null && configuration.MaxConcurrency is null)
        {
            configuration.MaxConcurrency = MaxConcurrency.Value;
        }
    }
}
