namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Default options for receive endpoints created by topology conventions.
/// </summary>
public sealed class AzureServiceBusDefaultEndpointOptions
{
    /// <summary>
    /// Gets or sets the default prefetch count for receive endpoints.
    /// Default is null (uses a computed default based on max concurrency).
    /// </summary>
    public int? PrefetchCount { get; set; }

    /// <summary>
    /// Gets or sets the default maximum number of messages to process concurrently.
    /// Default is null (uses the endpoint default of <see cref="Environment.ProcessorCount"/>).
    /// </summary>
    public int? MaxConcurrency { get; set; }

    /// <summary>
    /// Applies these defaults to a receive endpoint configuration, without overriding explicitly set values.
    /// </summary>
    internal void ApplyTo(AzureServiceBusReceiveEndpointConfiguration configuration)
    {
        if (PrefetchCount is not null && configuration.PrefetchCount <= 0)
        {
            configuration.PrefetchCount = PrefetchCount.Value;
        }

        if (MaxConcurrency is not null && configuration.MaxConcurrency is null)
        {
            configuration.MaxConcurrency = MaxConcurrency.Value;
        }
    }
}
