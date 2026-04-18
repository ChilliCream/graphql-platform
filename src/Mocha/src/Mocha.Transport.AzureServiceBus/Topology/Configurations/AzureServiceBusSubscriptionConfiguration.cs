namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Configuration for an Azure Service Bus subscription (topic-to-queue forwarding) in the messaging topology.
/// </summary>
public sealed class AzureServiceBusSubscriptionConfiguration : TopologyConfiguration<AzureServiceBusMessagingTopology>
{
    /// <summary>
    /// Gets or sets the source topic name.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the destination queue name.
    /// </summary>
    public string? Destination { get; set; }

    /// <summary>
    /// Gets or sets whether this subscription should be auto-provisioned.
    /// When true, the subscription will be created in Azure Service Bus during topology provisioning.
    /// </summary>
    public bool? AutoProvision { get; set; }

    /// <summary>
    /// Lock duration applied by the broker when a message is delivered to a receiver.
    /// </summary>
    public TimeSpan? LockDuration { get; set; }

    /// <summary>
    /// Maximum delivery attempts before a message is dead-lettered.
    /// </summary>
    public int? MaxDeliveryCount { get; set; }

    /// <summary>
    /// Default time-to-live applied to messages that do not specify their own.
    /// </summary>
    public TimeSpan? DefaultMessageTimeToLive { get; set; }

    /// <summary>
    /// Whether the subscription requires sessions.
    /// </summary>
    public bool? RequiresSession { get; set; }

    /// <summary>
    /// Entity to which messages received on this subscription are auto-forwarded.
    /// When unset, the subscription forwards to its destination queue by convention.
    /// </summary>
    public string? ForwardTo { get; set; }

    /// <summary>
    /// Entity to which dead-lettered messages from this subscription are auto-forwarded.
    /// </summary>
    public string? ForwardDeadLetteredMessagesTo { get; set; }

    /// <summary>
    /// Whether expired messages are moved to the dead-letter queue instead of being dropped.
    /// </summary>
    public bool? DeadLetteringOnMessageExpiration { get; set; }

    /// <summary>
    /// Idle window after which the broker may delete the subscription.
    /// </summary>
    public TimeSpan? AutoDeleteOnIdle { get; set; }
}
