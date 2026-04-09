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
}
