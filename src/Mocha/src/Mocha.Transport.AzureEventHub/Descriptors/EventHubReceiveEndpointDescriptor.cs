namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Descriptor for configuring an Event Hub receive endpoint that consumes messages from a specific hub.
/// </summary>
internal sealed class EventHubReceiveEndpointDescriptor
    : ReceiveEndpointDescriptor<EventHubReceiveEndpointConfiguration>
    , IEventHubReceiveEndpointDescriptor
{
    private EventHubReceiveEndpointDescriptor(IMessagingConfigurationContext discoveryContext, string name)
        : base(discoveryContext)
    {
        Configuration = new EventHubReceiveEndpointConfiguration { Name = name, HubName = name };
    }

    /// <inheritdoc />
    public new IEventHubReceiveEndpointDescriptor Handler<THandler>() where THandler : class, IHandler
    {
        base.Handler<THandler>();
        return this;
    }

    /// <inheritdoc />
    public new IEventHubReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        base.Consumer<TConsumer>();
        return this;
    }

    /// <inheritdoc />
    public new IEventHubReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind)
    {
        base.Kind(kind);
        return this;
    }

    /// <inheritdoc />
    public new IEventHubReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency)
    {
        base.MaxConcurrency(maxConcurrency);
        return this;
    }

    /// <inheritdoc />
    public IEventHubReceiveEndpointDescriptor Hub(string name)
    {
        Configuration.HubName = name;
        return this;
    }

    /// <inheritdoc />
    public IEventHubReceiveEndpointDescriptor ConsumerGroup(string consumerGroup)
    {
        Configuration.ConsumerGroup = consumerGroup;
        return this;
    }

    /// <inheritdoc />
    public IEventHubReceiveEndpointDescriptor CheckpointInterval(int interval)
    {
        Configuration.CheckpointInterval = interval;
        return this;
    }

    /// <inheritdoc />
    public new IEventHubReceiveEndpointDescriptor FaultEndpoint(string name)
    {
        base.FaultEndpoint(name);
        return this;
    }

    /// <inheritdoc />
    public new IEventHubReceiveEndpointDescriptor SkippedEndpoint(string name)
    {
        base.SkippedEndpoint(name);
        return this;
    }

    /// <inheritdoc />
    public new IEventHubReceiveEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseReceive(configuration, before, after);
        return this;
    }

    /// <summary>
    /// Builds the final receive endpoint configuration from the accumulated settings.
    /// </summary>
    /// <returns>The configured <see cref="EventHubReceiveEndpointConfiguration"/>.</returns>
    public EventHubReceiveEndpointConfiguration CreateConfiguration()
    {
        return Configuration;
    }

    /// <summary>
    /// Creates a new receive endpoint descriptor with the specified name.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The endpoint name and default hub name.</param>
    /// <returns>A new receive endpoint descriptor.</returns>
    public static EventHubReceiveEndpointDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
