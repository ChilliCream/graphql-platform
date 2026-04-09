namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Fluent descriptor for configuring an Azure Event Hub messaging transport, including endpoints, topology, and connection settings.
/// </summary>
public sealed class EventHubMessagingTransportDescriptor
    : MessagingTransportDescriptor<EventHubTransportConfiguration>
    , IEventHubMessagingTransportDescriptor
{
    private readonly List<EventHubReceiveEndpointDescriptor> _receiveEndpoints = [];
    private readonly List<EventHubDispatchEndpointDescriptor> _dispatchEndpoints = [];
    private readonly List<EventHubTopicDescriptor> _topics = [];
    private readonly List<EventHubSubscriptionDescriptor> _subscriptions = [];

    /// <summary>
    /// Creates a new Event Hub transport descriptor bound to the given setup context.
    /// </summary>
    /// <param name="discoveryContext">The messaging setup context used for handler and route discovery.</param>
    public EventHubMessagingTransportDescriptor(IMessagingSetupContext discoveryContext) : base(discoveryContext)
    {
        Configuration = new EventHubTransportConfiguration();
    }

    /// <inheritdoc />
    protected internal override EventHubTransportConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public new IEventHubMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure)
    {
        base.ModifyOptions(configure);
        return this;
    }

    /// <inheritdoc />
    public new IEventHubMessagingTransportDescriptor Name(string name)
    {
        base.Name(name);
        return this;
    }

    /// <inheritdoc />
    public new IEventHubMessagingTransportDescriptor AddConvention(IConvention convention)
    {
        base.AddConvention(convention);
        return this;
    }

    /// <inheritdoc />
    public new IEventHubMessagingTransportDescriptor IsDefaultTransport()
    {
        base.IsDefaultTransport();
        return this;
    }

    /// <inheritdoc />
    public new IEventHubMessagingTransportDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseDispatch(configuration, before, after);
        return this;
    }

    /// <inheritdoc />
    public new IEventHubMessagingTransportDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseReceive(configuration, before, after);
        return this;
    }

    /// <inheritdoc />
    public new IEventHubMessagingTransportDescriptor Schema(string schema)
    {
        base.Schema(schema);
        return this;
    }

    /// <inheritdoc />
    public new IEventHubMessagingTransportDescriptor BindHandlersImplicitly()
    {
        base.BindHandlersImplicitly();
        return this;
    }

    /// <inheritdoc />
    public new IEventHubMessagingTransportDescriptor BindHandlersExplicitly()
    {
        base.BindHandlersExplicitly();
        return this;
    }

    /// <inheritdoc />
    public IEventHubMessagingTransportDescriptor ConnectionString(string connectionString)
    {
        Configuration.ConnectionString = connectionString;
        return this;
    }

    /// <inheritdoc />
    public IEventHubMessagingTransportDescriptor Namespace(string fullyQualifiedNamespace)
    {
        Configuration.FullyQualifiedNamespace = fullyQualifiedNamespace;
        return this;
    }

    /// <inheritdoc />
    public IEventHubMessagingTransportDescriptor ConnectionProvider(
        Func<IServiceProvider, IEventHubConnectionProvider> connectionProvider)
    {
        Configuration.ConnectionProvider = connectionProvider;
        return this;
    }

    /// <inheritdoc />
    public IEventHubMessagingTransportDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <inheritdoc />
    public IEventHubMessagingTransportDescriptor ConfigureDefaults(Action<EventHubBusDefaults> configure)
    {
        configure(Configuration.Defaults);
        return this;
    }

    /// <inheritdoc />
    public IEventHubMessagingTransportDescriptor ResourceGroup(
        string subscriptionId,
        string resourceGroupName,
        string namespaceName)
    {
        Configuration.SubscriptionId = subscriptionId;
        Configuration.ResourceGroupName = resourceGroupName;
        Configuration.NamespaceName = namespaceName;
        return this;
    }

    /// <inheritdoc />
    public IEventHubReceiveEndpointDescriptor Endpoint(string name)
    {
        var endpoint = _receiveEndpoints.FirstOrDefault(e =>
            e.Extend().Configuration.Name.EqualsOrdinal(name)
            || e.Extend().Configuration.HubName.EqualsOrdinal(name));

        if (endpoint is null)
        {
            endpoint = EventHubReceiveEndpointDescriptor.New(Context, name);
            _receiveEndpoints.Add(endpoint);
        }

        return endpoint;
    }

    /// <inheritdoc />
    public IEventHubDispatchEndpointDescriptor DispatchEndpoint(string name)
    {
        var endpoint = _dispatchEndpoints.FirstOrDefault(e =>
            e.Extend().Configuration.Name.EqualsOrdinal(name));

        if (endpoint is null)
        {
            endpoint = EventHubDispatchEndpointDescriptor.New(Context, name);
            _dispatchEndpoints.Add(endpoint);
        }

        return endpoint;
    }

    /// <inheritdoc />
    public IEventHubTopicDescriptor DeclareTopic(string name)
    {
        var topic = _topics.FirstOrDefault(t =>
            t.Extend().Configuration.Name.EqualsOrdinal(name));

        if (topic is null)
        {
            topic = EventHubTopicDescriptor.New(Context, name);
            _topics.Add(topic);
        }

        return topic;
    }

    /// <inheritdoc />
    public IEventHubSubscriptionDescriptor DeclareSubscription(string topicName, string consumerGroup)
    {
        var subscription = _subscriptions.FirstOrDefault(s =>
            s.Extend().Configuration.TopicName.EqualsOrdinal(topicName)
            && s.Extend().Configuration.ConsumerGroup.EqualsOrdinal(consumerGroup));

        if (subscription is null)
        {
            subscription = EventHubSubscriptionDescriptor.New(Context, topicName, consumerGroup);
            _subscriptions.Add(subscription);
        }

        return subscription;
    }

    /// <inheritdoc />
    public IEventHubMessagingTransportDescriptor CheckpointStore(Func<IServiceProvider, ICheckpointStore> factory)
    {
        Configuration.CheckpointStoreFactory = factory;
        return this;
    }

    /// <inheritdoc />
    public IEventHubMessagingTransportDescriptor BlobCheckpointStore(
        string connectionString,
        string containerName)
    {
        Configuration.CheckpointStoreFactory = _ =>
        {
            var containerClient = new Azure.Storage.Blobs.BlobContainerClient(connectionString, containerName);
            return new BlobStorageCheckpointStore(containerClient);
        };
        return this;
    }

    /// <inheritdoc />
    public IEventHubMessagingTransportDescriptor OwnershipStore(
        Func<IServiceProvider, IPartitionOwnershipStore> factory)
    {
        Configuration.OwnershipStoreFactory = factory;
        return this;
    }

    /// <inheritdoc />
    public IEventHubMessagingTransportDescriptor BlobOwnershipStore(
        string connectionString,
        string containerName)
    {
        Configuration.OwnershipStoreFactory = _ =>
        {
            var containerClient = new Azure.Storage.Blobs.BlobContainerClient(connectionString, containerName);
            return new BlobStorageOwnershipStore(containerClient);
        };
        return this;
    }

    /// <summary>
    /// Builds the final transport configuration from all accumulated descriptor settings.
    /// </summary>
    /// <returns>A fully populated <see cref="EventHubTransportConfiguration"/> ready for transport initialization.</returns>
    public EventHubTransportConfiguration CreateConfiguration()
    {
        Configuration.ReceiveEndpoints = _receiveEndpoints
            .Select(ReceiveEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        Configuration.DispatchEndpoints = _dispatchEndpoints
            .Select(DispatchEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        Configuration.Topics = _topics.Select(t => t.CreateConfiguration()).ToList();

        Configuration.Subscriptions = _subscriptions.Select(s => s.CreateConfiguration()).ToList();

        return Configuration;
    }

    /// <summary>
    /// Creates a new Event Hub transport descriptor for the given setup context.
    /// </summary>
    /// <param name="discoveryContext">The messaging setup context.</param>
    /// <returns>A new transport descriptor instance.</returns>
    public static EventHubMessagingTransportDescriptor New(IMessagingSetupContext discoveryContext)
        => new(discoveryContext);
}
