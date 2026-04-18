namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Configuration for an Azure Service Bus queue in the messaging topology.
/// </summary>
public sealed class AzureServiceBusQueueConfiguration : TopologyConfiguration<AzureServiceBusMessagingTopology>
{
    /// <summary>
    /// Gets or sets the queue name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets whether this queue should be automatically deleted when no longer in use.
    /// </summary>
    public bool? AutoDelete { get; set; }

    /// <summary>
    /// Gets or sets whether this queue should be auto-provisioned.
    /// When true, the queue will be created in Azure Service Bus during topology provisioning.
    /// </summary>
    public bool? AutoProvision { get; set; }

    /// <summary>
    /// Idle window after which the broker may delete the queue. Only honored when set explicitly.
    /// </summary>
    public TimeSpan? AutoDeleteOnIdle { get; set; }

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
    /// Maximum queue size in megabytes (uses the SDK's <c>MaxSizeInMegabytes</c> contract).
    /// </summary>
    public long? MaxSizeInMegabytes { get; set; }

    /// <summary>
    /// Whether the queue requires sessions. Cannot be changed after the queue is created.
    /// </summary>
    public bool? RequiresSession { get; set; }

    /// <summary>
    /// Whether the queue is partitioned. Must be set at creation time.
    /// </summary>
    public bool? EnablePartitioning { get; set; }

    /// <summary>
    /// Entity to which messages received on this queue are auto-forwarded.
    /// </summary>
    public string? ForwardTo { get; set; }

    /// <summary>
    /// Entity to which dead-lettered messages from this queue are auto-forwarded.
    /// </summary>
    public string? ForwardDeadLetteredMessagesTo { get; set; }

    /// <summary>
    /// Whether expired messages are moved to the dead-letter queue instead of being dropped.
    /// </summary>
    public bool? DeadLetteringOnMessageExpiration { get; set; }
}
