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
}
