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
    /// When <see langword="null"/>, a default based on <c>MaxConcurrency * 2</c> is used.
    /// A value of zero disables prefetching.
    /// </summary>
    public int? PrefetchCount { get; set; }

    /// <summary>
    /// Gets or sets whether the endpoint's underlying queue forwards broker-dead-lettered messages
    /// (e.g. <c>MaxDeliveryCountExceeded</c>, <c>TTLExpiredException</c>) into the Mocha-managed
    /// <c>{queueName}_error</c> queue. Defaults to <see langword="false"/>.
    /// </summary>
    public bool UseNativeDeadLetterForwarding { get; set; }
}
