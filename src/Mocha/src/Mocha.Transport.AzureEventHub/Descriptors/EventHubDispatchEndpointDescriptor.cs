namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Descriptor for configuring an Event Hub dispatch endpoint that targets a hub for outbound message delivery.
/// </summary>
internal sealed class EventHubDispatchEndpointDescriptor
    : DispatchEndpointDescriptor<EventHubDispatchEndpointConfiguration>
    , IEventHubDispatchEndpointDescriptor
{
    private EventHubDispatchEndpointDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new EventHubDispatchEndpointConfiguration { Name = name, HubName = name };
    }

    /// <inheritdoc />
    public IEventHubDispatchEndpointDescriptor ToHub(string name)
    {
        Configuration.HubName = name;
        return this;
    }

    /// <inheritdoc />
    public IEventHubDispatchEndpointDescriptor PartitionId(string partitionId)
    {
        Configuration.PartitionId = partitionId;
        return this;
    }

    /// <inheritdoc />
    public IEventHubDispatchEndpointDescriptor BatchMode(EventHubBatchMode mode)
    {
        Configuration.BatchMode = mode;
        return this;
    }

    /// <inheritdoc />
    public new IEventHubDispatchEndpointDescriptor Send<TMessage>()
    {
        base.Send<TMessage>();
        return this;
    }

    /// <inheritdoc />
    public new IEventHubDispatchEndpointDescriptor Publish<TMessage>()
    {
        base.Publish<TMessage>();
        return this;
    }

    /// <inheritdoc />
    public new IEventHubDispatchEndpointDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseDispatch(configuration, before, after);
        return this;
    }

    /// <summary>
    /// Builds the final dispatch endpoint configuration from the accumulated settings.
    /// </summary>
    /// <returns>The configured <see cref="EventHubDispatchEndpointConfiguration"/>.</returns>
    public EventHubDispatchEndpointConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new dispatch endpoint descriptor with the specified name.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The endpoint name, also used as the default hub name.</param>
    /// <returns>A new dispatch endpoint descriptor.</returns>
    public static EventHubDispatchEndpointDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
