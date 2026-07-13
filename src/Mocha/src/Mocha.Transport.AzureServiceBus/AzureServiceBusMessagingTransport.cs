using System.Diagnostics.CodeAnalysis;
using Azure.Messaging.ServiceBus;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Azure Service Bus implementation of <see cref="MessagingTransport"/> that manages connections,
/// topology provisioning, and the lifecycle of receive and dispatch endpoints backed by
/// Azure Service Bus queues and topics.
/// </summary>
public sealed class AzureServiceBusMessagingTransport : MessagingTransport
{
    private readonly Action<IAzureServiceBusMessagingTransportDescriptor> _configure;

    /// <summary>
    /// Creates a new Azure Service Bus transport with the specified configuration delegate.
    /// </summary>
    /// <param name="configure">A delegate that configures the transport descriptor with endpoints, topology, and connection settings.</param>
    public AzureServiceBusMessagingTransport(Action<IAzureServiceBusMessagingTransportDescriptor> configure)
    {
        _configure = configure;
    }

    private AzureServiceBusMessagingTopology _topology = null!;

    /// <inheritdoc />
    public override MessagingTopology Topology => _topology;

    /// <summary>
    /// Gets the client manager responsible for managing the <c>ServiceBusClient</c> and cached senders.
    /// </summary>
    public AzureServiceBusClientManager ClientManager { get; private set; } = null!;

    /// <summary>
    /// Resolves the Azure Service Bus connection settings, creates the client manager, and builds the
    /// transport topology from the declared topics, queues, and subscriptions.
    /// </summary>
    /// <param name="context">The setup context providing access to the service provider and host configuration.</param>
    protected override void OnAfterInitialized(IMessagingSetupContext context)
    {
        var configuration = (AzureServiceBusTransportConfiguration)Configuration;

        ClientManager = new AzureServiceBusClientManager(configuration);

        var fullyQualifiedNamespace = configuration.FullyQualifiedNamespace;
        if (configuration.ConnectionString is { } connectionString)
        {
            fullyQualifiedNamespace =
                ServiceBusConnectionStringProperties.Parse(connectionString).FullyQualifiedNamespace;
        }

        var builder = new UriBuilder
        {
            Scheme = Schema,
            Host = fullyQualifiedNamespace ?? "localhost",
            Path = "/"
        };

        _topology = new AzureServiceBusMessagingTopology(
            this,
            builder.Uri,
            configuration.Defaults,
            configuration.AutoProvision ?? true);

        foreach (var topic in configuration.Topics)
        {
            _topology.AddTopic(topic);
        }

        foreach (var queue in configuration.Queues)
        {
            _topology.AddQueue(queue);
        }

        foreach (var subscription in configuration.Subscriptions)
        {
            _topology.AddSubscription(subscription);
        }
    }

    /// <summary>
    /// Provisions topology resources (topics, queues, subscriptions) on the Azure Service Bus namespace
    /// before the transport's endpoints begin processing messages.
    /// </summary>
    /// <param name="context">The configuration context for the current startup phase.</param>
    /// <param name="cancellationToken">A token to cancel the provisioning.</param>
    protected override async ValueTask OnBeforeStartAsync(
        IMessagingConfigurationContext context,
        CancellationToken cancellationToken)
    {
        var autoProvision = _topology.AutoProvision;

        foreach (var topic in _topology.Topics)
        {
            if (topic.AutoProvision ?? autoProvision)
            {
                await topic.ProvisionAsync(ClientManager, cancellationToken);
            }
        }

        var provisionedQueues = new HashSet<string>(StringComparer.Ordinal);
        var visitingQueues = new HashSet<string>(StringComparer.Ordinal);

        foreach (var queue in _topology.Queues)
        {
            await ProvisionQueueAsync(queue);
        }

        foreach (var subscription in _topology.Subscriptions)
        {
            if (subscription.AutoProvision ?? autoProvision)
            {
                await subscription.ProvisionAsync(ClientManager, cancellationToken);
            }
        }

        async ValueTask ProvisionQueueAsync(AzureServiceBusQueue queue)
        {
            if (provisionedQueues.Contains(queue.Name))
            {
                return;
            }

            if (!visitingQueues.Add(queue.Name))
            {
                throw new InvalidOperationException(
                    $"Azure Service Bus queue forwarding contains a cycle involving '{queue.Name}'.");
            }

            await ProvisionDependencyAsync(queue.ForwardTo);
            await ProvisionDependencyAsync(queue.ForwardDeadLetteredMessagesTo);

            visitingQueues.Remove(queue.Name);

            if (queue.AutoProvision ?? autoProvision)
            {
                await queue.ProvisionAsync(ClientManager, cancellationToken);
            }

            provisionedQueues.Add(queue.Name);
        }

        async ValueTask ProvisionDependencyAsync(string? entityName)
        {
            if (entityName is null)
            {
                return;
            }

            var dependency = _topology.Queues.FirstOrDefault(q => q.Name == entityName);
            if (dependency is not null)
            {
                await ProvisionQueueAsync(dependency);
            }
        }
    }

    /// <inheritdoc />
    public override TransportDescription Describe()
    {
        var receiveEndpoints = ReceiveEndpoints.Select(e => e.Describe()).ToList();
        var dispatchEndpoints = DispatchEndpoints.Select(e => e.Describe()).ToList();

        var entities = new List<TopologyEntityDescription>();
        var links = new List<TopologyLinkDescription>();
        var autoProvision = _topology.AutoProvision;

        foreach (var topic in _topology.Topics)
        {
            entities.Add(
                new TopologyEntityDescription(
                    MochaUrn.TopologyEntity(topic.Address?.ToString(), "topic", topic.Name),
                    "topic",
                    topic.Name,
                    topic.Address?.ToString(),
                    "both",
                    new Dictionary<string, object?>
                    {
                        ["autoProvision"] = topic.AutoProvision ?? autoProvision,
                        ["origin"] = topic.Origin
                    }));
        }

        foreach (var queue in _topology.Queues)
        {
            entities.Add(
                new TopologyEntityDescription(
                    MochaUrn.TopologyEntity(queue.Address?.ToString(), "queue", queue.Name),
                    "queue",
                    queue.Name,
                    queue.Address?.ToString(),
                    "both",
                    new Dictionary<string, object?>
                    {
                        ["autoDelete"] = queue.AutoDelete,
                        ["autoProvision"] = queue.AutoProvision ?? autoProvision,
                        ["origin"] = queue.Origin
                    }));
        }

        foreach (var subscription in _topology.Subscriptions)
        {
            var source = subscription.Source.Address?.ToString();
            var target = subscription.Destination.Address?.ToString();

            links.Add(
                new TopologyLinkDescription(
                    MochaUrn.TopologyLink(subscription.Address?.ToString(), "subscription", source, target),
                    "subscription",
                    subscription.Address?.ToString(),
                    source,
                    target,
                    "forward",
                    new Dictionary<string, object?>
                    {
                        ["name"] = subscription.Name,
                        ["autoProvision"] = subscription.AutoProvision ?? autoProvision,
                        ["origin"] = subscription.Origin
                    }));
        }

        var topology = new TopologyDescription(_topology.Address.ToString(), entities, links);

        return new TransportDescription(
            Urn,
            _topology.Address.ToString(),
            Name,
            Schema,
            nameof(AzureServiceBusMessagingTransport),
            receiveEndpoints,
            dispatchEndpoints,
            topology);
    }

    /// <inheritdoc />
    public override bool TryGetDispatchEndpoint(Uri address, [NotNullWhen(true)] out DispatchEndpoint? endpoint)
    {
        if (TryGetReplyDispatchEndpoint(address, out endpoint))
        {
            return true;
        }

        if (address.Scheme == Schema)
        {
            foreach (var candidate in DispatchEndpoints)
            {
                if (!candidate.IsCompleted)
                {
                    continue;
                }

                if (candidate.Address == address)
                {
                    endpoint = candidate;
                    return true;
                }
            }
        }

        if (Topology.Address.IsBaseOf(address))
        {
            foreach (var candidate in DispatchEndpoints)
            {
                if (!candidate.IsCompleted)
                {
                    continue;
                }

                if (candidate.Destination.Address == address)
                {
                    endpoint = candidate;
                    return true;
                }
            }
        }

        if (TryGetResourceName(address, "queue", out var queueName))
        {
            foreach (var candidate in DispatchEndpoints)
            {
                if (!candidate.IsCompleted)
                {
                    continue;
                }

                if (candidate.Destination is AzureServiceBusQueue queue && queue.Name == queueName)
                {
                    endpoint = candidate;
                    return true;
                }
            }
        }

        if (TryGetResourceName(address, "topic", out var topicName))
        {
            foreach (var candidate in DispatchEndpoints)
            {
                if (!candidate.IsCompleted)
                {
                    continue;
                }

                if (candidate.Destination is AzureServiceBusTopic topic && topic.Name == topicName)
                {
                    endpoint = candidate;
                    return true;
                }
            }
        }

        endpoint = null;
        return false;
    }

    /// <summary>
    /// Builds the Azure Service Bus-specific transport configuration by invoking the user-supplied
    /// configuration delegate on an <see cref="AzureServiceBusMessagingTransportDescriptor"/>.
    /// </summary>
    /// <param name="context">The setup context providing access to the service provider and host configuration.</param>
    /// <returns>A <see cref="MessagingTransportConfiguration"/> containing all Azure Service Bus endpoint and pipeline definitions.</returns>
    protected override MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext context)
    {
        var descriptor = new AzureServiceBusMessagingTransportDescriptor(context);

        _configure(descriptor);

        return descriptor.CreateConfiguration();
    }

    /// <summary>
    /// Creates a new <see cref="AzureServiceBusReceiveEndpoint"/> bound to this transport.
    /// </summary>
    /// <returns>A new, uninitialized <see cref="AzureServiceBusReceiveEndpoint"/> for this transport.</returns>
    protected override ReceiveEndpoint CreateReceiveEndpoint()
    {
        return new AzureServiceBusReceiveEndpoint(this);
    }

    /// <summary>
    /// Creates a new <see cref="AzureServiceBusDispatchEndpoint"/> bound to this transport.
    /// </summary>
    /// <returns>A new, uninitialized <see cref="AzureServiceBusDispatchEndpoint"/> for this transport.</returns>
    protected override DispatchEndpoint CreateDispatchEndpoint()
    {
        return new AzureServiceBusDispatchEndpoint(this);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (ClientManager is not null)
        {
            await ClientManager.DisposeAsync();
        }
    }
}
