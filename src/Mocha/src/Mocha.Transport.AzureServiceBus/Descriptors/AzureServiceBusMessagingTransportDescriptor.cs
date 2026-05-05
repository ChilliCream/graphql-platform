using Azure.Core;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Configures an Azure Service Bus messaging transport, including its endpoints, topics, queues, and subscriptions.
/// </summary>
/// <remarks>
/// This descriptor collects all receive/dispatch endpoint, topic, queue, and subscription declarations
/// during setup and materializes them into an <see cref="AzureServiceBusTransportConfiguration"/> via
/// <see cref="CreateConfiguration"/>. Use the fluent API to compose transport-level middleware,
/// naming, and handler binding strategies before the configuration is finalized.
/// </remarks>
public sealed class AzureServiceBusMessagingTransportDescriptor
    : MessagingTransportDescriptor<AzureServiceBusTransportConfiguration>
    , IAzureServiceBusMessagingTransportDescriptor
{
    private readonly List<AzureServiceBusReceiveEndpointDescriptor> _receiveEndpoints = [];
    private readonly List<AzureServiceBusDispatchEndpointDescriptor> _dispatchEndpoints = [];
    private readonly List<AzureServiceBusTopicDescriptor> _topics = [];
    private readonly List<AzureServiceBusQueueDescriptor> _queues = [];
    private readonly List<AzureServiceBusSubscriptionDescriptor> _subscriptions = [];

    /// <summary>
    /// Creates a new Azure Service Bus transport descriptor bound to the specified setup context.
    /// </summary>
    /// <param name="discoveryContext">The messaging setup context used to discover handlers and routes.</param>
    public AzureServiceBusMessagingTransportDescriptor(IMessagingSetupContext discoveryContext) : base(discoveryContext)
    {
        Configuration = new AzureServiceBusTransportConfiguration();
    }

    protected internal override AzureServiceBusTransportConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public new IAzureServiceBusMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure)
    {
        base.ModifyOptions(configure);

        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusMessagingTransportDescriptor Name(string name)
    {
        base.Name(name);

        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusMessagingTransportDescriptor AddConvention(IConvention convention)
    {
        base.AddConvention(convention);

        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusMessagingTransportDescriptor IsDefaultTransport()
    {
        base.IsDefaultTransport();

        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusMessagingTransportDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseDispatch(configuration, before: before, after: after);

        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusMessagingTransportDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseReceive(configuration, before: before, after: after);

        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusMessagingTransportDescriptor Schema(string schema)
    {
        base.Schema(schema);

        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusMessagingTransportDescriptor BindHandlersImplicitly()
    {
        base.BindHandlersImplicitly();

        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusMessagingTransportDescriptor BindHandlersExplicitly()
    {
        base.BindHandlersExplicitly();

        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusMessagingTransportDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusMessagingTransportDescriptor ConnectionString(string connectionString)
    {
        Configuration.ConnectionString = connectionString;

        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusMessagingTransportDescriptor Namespace(
        string fullyQualifiedNamespace,
        TokenCredential credential)
    {
        Configuration.FullyQualifiedNamespace = fullyQualifiedNamespace;
        Configuration.Credential = credential;

        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusMessagingTransportDescriptor ConfigureDefaults(Action<AzureServiceBusBusDefaults> configure)
    {
        configure(Configuration.Defaults);

        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusReceiveEndpointDescriptor Endpoint(string name)
    {
        var endpoint = _receiveEndpoints.FirstOrDefault(e =>
            e.Extend().Configuration.Name.EqualsOrdinal(name) || e.Extend().Configuration.QueueName.EqualsOrdinal(name)
        );

        if (endpoint is null)
        {
            endpoint = AzureServiceBusReceiveEndpointDescriptor.New(Context, name);
            _receiveEndpoints.Add(endpoint);
        }

        return endpoint;
    }

    /// <inheritdoc />
    public IAzureServiceBusDispatchEndpointDescriptor DispatchEndpoint(string name)
    {
        var endpoint = _dispatchEndpoints.FirstOrDefault(e => e.Extend().Configuration.Name.EqualsOrdinal(name));
        if (endpoint is null)
        {
            endpoint = AzureServiceBusDispatchEndpointDescriptor.New(Context, name);
            _dispatchEndpoints.Add(endpoint);
        }

        return endpoint;
    }

    /// <inheritdoc />
    public IAzureServiceBusTopicDescriptor DeclareTopic(string name)
    {
        var topic = _topics.FirstOrDefault(e => e.Extend().Configuration.Name.EqualsOrdinal(name));
        if (topic is null)
        {
            topic = AzureServiceBusTopicDescriptor.New(Context, name);
            _topics.Add(topic);
        }

        return topic;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueDescriptor DeclareQueue(string name)
    {
        var queue = _queues.FirstOrDefault(q => q.Extend().Configuration.Name.EqualsOrdinal(name));
        if (queue is null)
        {
            queue = AzureServiceBusQueueDescriptor.New(Context, name);
            _queues.Add(queue);
        }

        return queue;
    }

    /// <inheritdoc />
    public IAzureServiceBusSubscriptionDescriptor DeclareSubscription(string topic, string queue)
    {
        var subscription = _subscriptions.FirstOrDefault(b =>
            b.Extend().Configuration.Source.EqualsOrdinal(topic)
            && b.Extend().Configuration.Destination.EqualsOrdinal(queue)
        );

        if (subscription is null)
        {
            subscription = AzureServiceBusSubscriptionDescriptor.New(Context, topic, queue);
            _subscriptions.Add(subscription);
        }

        return subscription;
    }

    /// <summary>
    /// Builds the final <see cref="AzureServiceBusTransportConfiguration"/> from all declared endpoints,
    /// topics, queues, and subscriptions.
    /// </summary>
    /// <returns>The fully populated transport configuration ready for runtime initialization.</returns>
    public AzureServiceBusTransportConfiguration CreateConfiguration()
    {
        Configuration.ReceiveEndpoints = _receiveEndpoints
            .Select(ReceiveEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        Configuration.DispatchEndpoints = _dispatchEndpoints
            .Select(DispatchEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        Configuration.Topics = _topics.Select(e => e.CreateConfiguration()).ToList();

        Configuration.Queues = _queues.Select(q => q.CreateConfiguration()).ToList();

        Configuration.Subscriptions = _subscriptions.Select(b => b.CreateConfiguration()).ToList();

        return Configuration;
    }

    /// <summary>
    /// Factory method that creates a new <see cref="AzureServiceBusMessagingTransportDescriptor"/>.
    /// </summary>
    /// <param name="discoveryContext">The messaging setup context used to discover handlers and routes.</param>
    /// <returns>A new transport descriptor instance.</returns>
    public static AzureServiceBusMessagingTransportDescriptor New(IMessagingSetupContext discoveryContext)
        => new(discoveryContext);
}
