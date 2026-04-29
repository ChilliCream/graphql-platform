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

    /// <summary>
    /// Gets or sets the maximum number of concurrently locked sessions on a session-bound endpoint.
    /// When <see langword="null"/>, the endpoint falls back to <see cref="ReceiveEndpointConfiguration.MaxConcurrency"/>.
    /// </summary>
    public int? MaxConcurrentSessions { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent message dispatches per locked session on a
    /// session-bound endpoint. When <see langword="null"/>, defaults to <c>1</c> to preserve
    /// in-session ordering.
    /// </summary>
    public int? MaxConcurrentCallsPerSession { get; set; }

    /// <summary>
    /// Gets or sets the duration the session processor will wait for new messages on a locked
    /// session before releasing the session lock. When <see langword="null"/>, the SDK default applies.
    /// </summary>
    public TimeSpan? SessionIdleTimeout { get; set; }

    /// <summary>
    /// Gets or sets the maximum total duration over which the SDK will auto-renew the message
    /// lock (and, on session endpoints, the session lock as well). When <see langword="null"/>,
    /// the endpoint default of five minutes is used.
    /// </summary>
    public TimeSpan? MaxAutoLockRenewalDuration { get; set; }
}
