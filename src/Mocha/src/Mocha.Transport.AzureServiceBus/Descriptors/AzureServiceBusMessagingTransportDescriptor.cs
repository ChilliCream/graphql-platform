using Azure.Core;
using Mocha.Features;

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
    private readonly List<AzureServiceBusQueueTopologyDescriptor> _queueTopology = [];
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
    public new IAzureServiceBusMessagingTransportDescriptor UseRoutingStrategy(
        Func<IServiceProvider, RoutingStrategy> factory)
    {
        base.UseRoutingStrategy(factory);

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
    public new IAzureServiceBusMessagingTransportDescriptor BindImplicitly()
    {
        base.BindImplicitly();

        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusMessagingTransportDescriptor BindExplicitly()
    {
        base.BindExplicitly();

        return this;
    }

    /// <inheritdoc />
    [Obsolete("Use BindImplicitly() instead.")]
    public IAzureServiceBusMessagingTransportDescriptor BindHandlersImplicitly()
        => BindImplicitly();

    /// <inheritdoc />
    [Obsolete("Use BindExplicitly() instead.")]
    public IAzureServiceBusMessagingTransportDescriptor BindHandlersExplicitly()
        => BindExplicitly();

    /// <inheritdoc />
    public IMessagingTransportHandlerDescriptor<IAzureServiceBusReceiveEndpointDescriptor> Handler<THandler>()
        where THandler : class, IHandler
    {
        var name = Context.Naming.GetReceiveEndpointName(typeof(THandler), ReceiveEndpointKind.Default);
        var endpoint = Endpoint(name);
        endpoint.Handler(typeof(THandler));
        return new MessagingTransportHandlerDescriptor<IAzureServiceBusReceiveEndpointDescriptor>(endpoint);
    }

    /// <inheritdoc />
    public IMessagingTransportConsumerDescriptor<IAzureServiceBusReceiveEndpointDescriptor> Consumer<TConsumer>()
        where TConsumer : class, IConsumer
    {
        var name = Context.Naming.GetReceiveEndpointName(typeof(TConsumer), ReceiveEndpointKind.Default);
        var endpoint = Endpoint(name);
        endpoint.Consumer(typeof(TConsumer));
        return new MessagingTransportConsumerDescriptor<IAzureServiceBusReceiveEndpointDescriptor>(endpoint);
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
    public IAzureServiceBusQueueTopologyDescriptor DeclareQueue(string name)
    {
        var queue = _queueTopology.FirstOrDefault(q => q.Extend().Configuration.Name.EqualsOrdinal(name));
        if (queue is null)
        {
            queue = AzureServiceBusQueueTopologyDescriptor.New(Context, name);
            _queueTopology.Add(queue);
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

    /// <inheritdoc />
    public IAzureServiceBusQueueDescriptor Queue(string name)
    {
        var queue = _queues.FirstOrDefault(q => q.Extend().Configuration.Name.EqualsOrdinal(name));
        if (queue is not null)
        {
            return queue;
        }

        queue = AzureServiceBusQueueDescriptor.New(Context, name);
        _queues.Add(queue);
        return queue;
    }

    /// <summary>
    /// Builds the final <see cref="AzureServiceBusTransportConfiguration"/> from all declared endpoints,
    /// topics, queues, and subscriptions.
    /// </summary>
    /// <returns>The fully populated transport configuration ready for runtime initialization.</returns>
    public AzureServiceBusTransportConfiguration CreateConfiguration()
    {
        foreach (var queue in _queues.Select(q => q.CreateConfiguration()))
        {
            ConfigureQueueTopology(queue);
            ConfigureQueueEndpoint(queue);
        }

        Configuration.ReceiveEndpoints = _receiveEndpoints
            .Select(ReceiveEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        Configuration.DispatchEndpoints = _dispatchEndpoints
            .Select(DispatchEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        Configuration.Topics = _topics.Select(e => e.CreateConfiguration()).ToList();

        Configuration.Queues = _queueTopology.Select(q => q.CreateConfiguration()).ToList();

        Configuration.Subscriptions = _subscriptions.Select(b => b.CreateConfiguration()).ToList();

        return Configuration;
    }

    private void ConfigureQueueTopology(AzureServiceBusQueueDescriptorConfiguration configuration)
    {
        var queue = DeclareQueue(configuration.Name);
        ApplyQueueConfiguration(configuration.Queue, queue);

        var schema = Configuration.Schema ?? AzureServiceBusTransportConfiguration.DefaultSchema;
        foreach (var source in configuration.SourceTopics)
        {
            if (!AzureServiceBusDestinations.TryResolveSourceTopic(schema, source, out var topicName))
            {
                throw new InvalidOperationException(
                    $"BindFrom source '{source}' could not be resolved to an Azure Service Bus topic name.");
            }

            DeclareTopic(topicName);
            DeclareSubscription(topicName, configuration.Name);
        }
    }

    private void ConfigureQueueEndpoint(AzureServiceBusQueueDescriptorConfiguration configuration)
    {
        var endpoint = Endpoint(configuration.Name);
        var target = endpoint.Extend().Configuration;

        target.ConsumerIdentities.AddRange(configuration.ConsumerIdentities);
        target.ReceivedMessageTypes.AddRange(configuration.ReceivedMessageTypes);
        target.BindMode = configuration.BindMode ?? target.BindMode;

        if (configuration.Kind is not null && target.Kind == ReceiveEndpointKind.Default)
        {
            target.Kind = configuration.Kind.Value;
        }

        if (configuration.MaxConcurrency is not null)
        {
            target.MaxConcurrency ??= configuration.MaxConcurrency;
        }

        if (configuration.PrefetchCount is not null)
        {
            target.PrefetchCount ??= configuration.PrefetchCount;
        }

        target.UseNativeDeadLetterForwarding |= configuration.UseNativeDeadLetterForwarding;
        target.MaxConcurrentSessions ??= configuration.MaxConcurrentSessions;
        target.MaxConcurrentCallsPerSession ??= configuration.MaxConcurrentCallsPerSession;
        target.SessionIdleTimeout ??= configuration.SessionIdleTimeout;
        target.MaxAutoLockRenewalDuration ??= configuration.MaxAutoLockRenewalDuration;
        target.ReceiveMiddlewares.AddRange(configuration.ReceiveMiddlewares);
        target.ReceivePipelineModifiers.AddRange(configuration.ReceivePipelineModifiers);

        CopyFaultEndpointFeature(configuration, target);
        CopySkippedEndpointFeature(configuration, target);
    }

    private static void CopyFaultEndpointFeature(
        AzureServiceBusQueueDescriptorConfiguration configuration,
        AzureServiceBusReceiveEndpointConfiguration target)
    {
        var source = configuration.Features.Get<ReceiveFaultEndpointFeature>();
        if (source is null)
        {
            return;
        }

        var targetFeature = target.Features.GetOrSet<ReceiveFaultEndpointFeature>();
        if (targetFeature is { Address: not null } or { IsDisabled: true })
        {
            return;
        }

        targetFeature.Address = source.Address;
        targetFeature.IsDisabled = source.IsDisabled;
    }

    private static void CopySkippedEndpointFeature(
        AzureServiceBusQueueDescriptorConfiguration configuration,
        AzureServiceBusReceiveEndpointConfiguration target)
    {
        var source = configuration.Features.Get<ReceiveSkippedEndpointFeature>();
        if (source is null)
        {
            return;
        }

        var targetFeature = target.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
        if (targetFeature is { Address: not null } or { IsDisabled: true })
        {
            return;
        }

        targetFeature.Address = source.Address;
        targetFeature.IsDisabled = source.IsDisabled;
    }

    private static void ApplyQueueConfiguration(
        AzureServiceBusQueueConfiguration configuration,
        IAzureServiceBusQueueTopologyDescriptor descriptor)
    {
        if (configuration.AutoDelete is { } autoDelete)
        {
            descriptor.AutoDelete(autoDelete);
        }

        if (configuration.AutoProvision is { } autoProvision)
        {
            descriptor.AutoProvision(autoProvision);
        }

        if (configuration.AutoDeleteOnIdle is { } autoDeleteOnIdle)
        {
            descriptor.WithAutoDeleteOnIdle(autoDeleteOnIdle);
        }

        if (configuration.LockDuration is { } lockDuration)
        {
            descriptor.WithLockDuration(lockDuration);
        }

        if (configuration.MaxDeliveryCount is { } maxDeliveryCount)
        {
            descriptor.WithMaxDeliveryCount(maxDeliveryCount);
        }

        if (configuration.DefaultMessageTimeToLive is { } timeToLive)
        {
            descriptor.WithDefaultMessageTimeToLive(timeToLive);
        }

        if (configuration.MaxSizeInMegabytes is { } maxSize)
        {
            descriptor.WithMaxSizeInMegabytes(maxSize);
        }

        if (configuration.RequiresSession is { } requiresSession)
        {
            descriptor.WithRequiresSession(requiresSession);
        }

        if (configuration.EnablePartitioning is { } enablePartitioning)
        {
            descriptor.WithEnablePartitioning(enablePartitioning);
        }

        if (configuration.ForwardTo is { } forwardTo)
        {
            descriptor.WithForwardTo(forwardTo);
        }

        if (configuration.ForwardDeadLetteredMessagesTo is { } forwardDeadLettersTo)
        {
            descriptor.WithForwardDeadLetteredMessagesTo(forwardDeadLettersTo);
        }

        if (configuration.DeadLetteringOnMessageExpiration is { } deadLetterOnExpiration)
        {
            descriptor.WithDeadLetteringOnMessageExpiration(deadLetterOnExpiration);
        }
    }

    /// <summary>
    /// Factory method that creates a new <see cref="AzureServiceBusMessagingTransportDescriptor"/>.
    /// </summary>
    /// <param name="discoveryContext">The messaging setup context used to discover handlers and routes.</param>
    /// <returns>A new transport descriptor instance.</returns>
    public static AzureServiceBusMessagingTransportDescriptor New(IMessagingSetupContext discoveryContext)
        => new(discoveryContext);
}
