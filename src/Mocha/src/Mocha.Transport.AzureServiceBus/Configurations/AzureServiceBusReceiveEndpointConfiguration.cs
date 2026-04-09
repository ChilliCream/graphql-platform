namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Configuration for an Azure Service Bus receive endpoint, specifying the source queue,
/// concurrency settings, and prefetch behavior.
/// </summary>
public sealed class AzureServiceBusReceiveEndpointConfiguration : ReceiveEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the Azure Service Bus queue name from which this endpoint consumes messages.
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Gets or sets the number of messages to prefetch from the broker.
    /// When zero or negative, a default based on <c>MaxConcurrency * 2</c> is used.
    /// </summary>
    public int PrefetchCount { get; set; }
}
