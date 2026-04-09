namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Represents a consumer group on an Event Hub as a topology resource.
/// </summary>
public sealed class EventHubSubscription : TopologyResource<EventHubSubscriptionConfiguration>
{
    /// <summary>
    /// Gets the name of the Event Hub entity (topic) this subscription belongs to.
    /// </summary>
    public string TopicName { get; private set; } = null!;

    /// <summary>
    /// Gets the consumer group name.
    /// </summary>
    public string ConsumerGroup { get; private set; } = null!;

    /// <summary>
    /// Gets a value indicating whether this consumer group is automatically provisioned during topology setup.
    /// When <c>null</c>, the transport-level default is used.
    /// </summary>
    public bool? AutoProvision { get; private set; }

    /// <inheritdoc />
    protected override void OnInitialize(EventHubSubscriptionConfiguration configuration)
    {
        TopicName = configuration.TopicName;
        ConsumerGroup = configuration.ConsumerGroup;
        AutoProvision = configuration.AutoProvision;
    }

    /// <summary>
    /// Provisions this consumer group using the specified provisioner.
    /// </summary>
    /// <param name="provisioner">The provisioner to use for ARM operations.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    internal async Task ProvisionAsync(EventHubProvisioner provisioner, CancellationToken cancellationToken)
    {
        await provisioner.ProvisionSubscriptionAsync(TopicName, ConsumerGroup, cancellationToken);
    }

    /// <inheritdoc />
    protected override void OnComplete(EventHubSubscriptionConfiguration configuration)
    {
        var address = new UriBuilder(Topology.Address);
        var basePath = address.Path.TrimEnd('/');
        address.Path = basePath + "/h/" + configuration.TopicName + "/cg/" + configuration.ConsumerGroup;
        Address = address.Uri;
    }
}
