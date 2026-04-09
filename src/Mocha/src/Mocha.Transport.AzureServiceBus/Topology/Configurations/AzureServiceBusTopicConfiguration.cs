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
}
