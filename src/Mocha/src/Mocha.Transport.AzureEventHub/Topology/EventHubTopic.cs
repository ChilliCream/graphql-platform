namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Represents an Event Hub entity (topic) as a topology resource.
/// </summary>
public sealed class EventHubTopic : TopologyResource<EventHubTopicConfiguration>
{
    /// <summary>
    /// Gets the name of this Event Hub entity.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the number of partitions for this Event Hub, or <c>null</c> if using the Azure default.
    /// </summary>
    public int? PartitionCount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this topic is automatically provisioned during topology setup.
    /// When <c>null</c>, the transport-level default is used.
    /// </summary>
    public bool? AutoProvision { get; private set; }

    /// <inheritdoc />
    protected override void OnInitialize(EventHubTopicConfiguration configuration)
    {
        Name = configuration.Name;
        PartitionCount = configuration.PartitionCount;
        AutoProvision = configuration.AutoProvision;
    }

    /// <summary>
    /// Provisions this Event Hub entity using the specified provisioner.
    /// </summary>
    /// <param name="provisioner">The provisioner to use for ARM operations.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    internal async Task ProvisionAsync(EventHubProvisioner provisioner, CancellationToken cancellationToken)
    {
        await provisioner.ProvisionTopicAsync(Name, PartitionCount, cancellationToken);
    }

    /// <inheritdoc />
    protected override void OnComplete(EventHubTopicConfiguration configuration)
    {
        var address = new UriBuilder(Topology.Address);
        var basePath = address.Path.TrimEnd('/');
        address.Path = basePath + "/h/" + configuration.Name;
        Address = address.Uri;
    }
}
