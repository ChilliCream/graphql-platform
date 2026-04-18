namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Configuration for an Azure Service Bus topic in the messaging topology.
/// </summary>
public sealed class AzureServiceBusTopicConfiguration : TopologyConfiguration<AzureServiceBusMessagingTopology>
{
    /// <summary>
    /// Gets or sets the topic name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets whether this topic should be auto-provisioned.
    /// When true, the topic will be created in Azure Service Bus during topology provisioning.
    /// </summary>
    public bool? AutoProvision { get; set; }

    /// <summary>
    /// Default time-to-live applied to messages that do not specify their own.
    /// </summary>
    public TimeSpan? DefaultMessageTimeToLive { get; set; }

    /// <summary>
    /// Maximum topic size in megabytes (uses the SDK's <c>MaxSizeInMegabytes</c> contract).
    /// </summary>
    public long? MaxSizeInMegabytes { get; set; }

    /// <summary>
    /// Whether the topic is partitioned. Must be set at creation time.
    /// </summary>
    public bool? EnablePartitioning { get; set; }

    /// <summary>
    /// Whether the topic enforces duplicate detection across the configured history window.
    /// </summary>
    public bool? RequiresDuplicateDetection { get; set; }

    /// <summary>
    /// Time window over which duplicate detection is performed.
    /// </summary>
    public TimeSpan? DuplicateDetectionHistoryTimeWindow { get; set; }

    /// <summary>
    /// Idle window after which the broker may delete the topic.
    /// </summary>
    public TimeSpan? AutoDeleteOnIdle { get; set; }

    /// <summary>
    /// Whether the topic preserves ordering across partitioned subscriptions.
    /// </summary>
    public bool? SupportOrdering { get; set; }
}
