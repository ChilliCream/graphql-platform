namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Configuration collected by <see cref="IAzureServiceBusQueueDescriptor"/>.
/// </summary>
public sealed class AzureServiceBusQueueDescriptorConfiguration : MessagingConfiguration
{
    internal AzureServiceBusQueueDescriptorConfiguration(string name)
    {
        Name = name;
        Queue = new AzureServiceBusQueueConfiguration
        {
            Name = name,
            Origin = TopologyOrigin.Declared
        };
    }

    /// <summary>
    /// Gets the queue name, which also serves as the receive endpoint identity.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the backing queue topology configuration.
    /// </summary>
    public AzureServiceBusQueueConfiguration Queue { get; }

    /// <summary>
    /// Gets the consumer identities explicitly attached to the queue.
    /// </summary>
    public List<Type> ConsumerIdentities { get; } = [];

    /// <summary>
    /// Gets the message types received by the queue.
    /// </summary>
    public List<Type> ReceivedMessageTypes { get; } = [];

    /// <summary>
    /// Gets or sets the queue-scoped bind mode.
    /// </summary>
    public MessagingBindMode? BindMode { get; set; }

    /// <summary>
    /// Gets or sets the receive endpoint kind.
    /// </summary>
    public ReceiveEndpointKind? Kind { get; set; }

    /// <summary>
    /// Gets or sets the receive endpoint maximum concurrency.
    /// </summary>
    public int? MaxConcurrency { get; set; }

    /// <summary>
    /// Gets or sets the Service Bus receiver prefetch count.
    /// </summary>
    public int? PrefetchCount { get; set; }

    /// <summary>
    /// Gets or sets whether native dead letters are forwarded to the fault endpoint queue.
    /// </summary>
    public bool UseNativeDeadLetterForwarding { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent sessions.
    /// </summary>
    public int? MaxConcurrentSessions { get; set; }

    /// <summary>
    /// Gets or sets the maximum concurrent calls per session.
    /// </summary>
    public int? MaxConcurrentCallsPerSession { get; set; }

    /// <summary>
    /// Gets or sets the session idle timeout.
    /// </summary>
    public TimeSpan? SessionIdleTimeout { get; set; }

    /// <summary>
    /// Gets or sets the maximum automatic lock-renewal duration.
    /// </summary>
    public TimeSpan? MaxAutoLockRenewalDuration { get; set; }

    /// <summary>
    /// Gets the receive middleware configurations applied to the materialized endpoint.
    /// </summary>
    public List<ReceiveMiddlewareConfiguration> ReceiveMiddlewares { get; } = [];

    /// <summary>
    /// Gets the receive pipeline modifiers applied to the materialized endpoint.
    /// </summary>
    public List<Action<List<ReceiveMiddlewareConfiguration>>> ReceivePipelineModifiers { get; } = [];

    /// <summary>
    /// Gets the explicitly declared source topics.
    /// </summary>
    public List<Uri> SourceTopics { get; } = [];
}
