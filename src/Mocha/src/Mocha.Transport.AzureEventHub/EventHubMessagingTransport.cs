using System.Diagnostics.CodeAnalysis;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static System.StringSplitOptions;

namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Azure Event Hub implementation of <see cref="MessagingTransport"/> that manages connections, topology
/// and the lifecycle of receive and dispatch endpoints backed by Event Hub entities.
/// </summary>
public sealed class EventHubMessagingTransport : MessagingTransport
{
    private readonly Action<IEventHubMessagingTransportDescriptor> _configure;
    private EventHubMessagingTopology _topology = null!;
    private EventHubTransportConfiguration _transportConfiguration = null!;

    /// <summary>
    /// Creates a new Event Hub transport with the specified configuration delegate.
    /// </summary>
    /// <param name="configure">A delegate that configures the transport descriptor.</param>
    public EventHubMessagingTransport(Action<IEventHubMessagingTransportDescriptor> configure)
    {
        _configure = configure;
    }

    /// <inheritdoc />
    public override MessagingTopology Topology => _topology;

    /// <inheritdoc />
    public override MessagingTransportCapabilities Capabilities
        => MessagingTransportCapabilities.Send | MessagingTransportCapabilities.PublishSubscribe;

    /// <summary>
    /// Gets the connection manager responsible for managing singleton producer clients per hub.
    /// </summary>
    public EventHubConnectionManager ConnectionManager { get; private set; } = null!;

    /// <summary>
    /// Gets the typed Event Hub transport configuration.
    /// Captured during initialization so it survives the base class Finalize() null-out.
    /// </summary>
    internal EventHubTransportConfiguration TransportConfiguration => _transportConfiguration;

    /// <inheritdoc />
    protected override MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext context)
    {
        var descriptor = new EventHubMessagingTransportDescriptor(context);
        _configure(descriptor);
        return descriptor.CreateConfiguration();
    }

    /// <summary>
    /// Resolves the Event Hub connection provider, builds the transport topology URI, and creates
    /// the <see cref="ConnectionManager"/> instance used for the lifetime of this transport.
    /// </summary>
    /// <param name="context">The setup context providing access to the service provider and host configuration.</param>
    protected override void OnAfterInitialized(IMessagingSetupContext context)
    {
        var configuration = (EventHubTransportConfiguration)Configuration;
        _transportConfiguration = configuration;

        var connectionProvider = configuration.ConnectionProvider?.Invoke(context.Services)
            ?? ResolveDefaultConnectionProvider(configuration);

        var builder = new UriBuilder
        {
            Scheme = Schema,
            Host = connectionProvider.FullyQualifiedNamespace
        };

        _topology = new EventHubMessagingTopology(
            this,
            builder.Uri,
            configuration.Defaults,
            configuration.AutoProvision ?? true);

        foreach (var topic in configuration.Topics)
        {
            _topology.AddTopic(topic);
        }

        foreach (var subscription in configuration.Subscriptions)
        {
            _topology.AddSubscription(subscription);
        }

        ConnectionManager = new EventHubConnectionManager(
            context.Services.GetRequiredService<ILogger<EventHubConnectionManager>>(),
            connectionProvider);
    }

    private static IEventHubConnectionProvider ResolveDefaultConnectionProvider(
        EventHubTransportConfiguration configuration)
    {
        if (configuration.ConnectionString is not null)
        {
            return new ConnectionStringEventHubConnectionProvider(configuration.ConnectionString);
        }

        if (configuration.FullyQualifiedNamespace is not null)
        {
            return new CredentialEventHubConnectionProvider(
                configuration.FullyQualifiedNamespace,
                new DefaultAzureCredential());
        }

        throw new InvalidOperationException(
            "Event Hub transport requires either a ConnectionString, FullyQualifiedNamespace, or ConnectionProvider.");
    }

    /// <inheritdoc />
    protected override async ValueTask OnBeforeStartAsync(
        IMessagingConfigurationContext context,
        CancellationToken cancellationToken)
    {
        var autoProvision = _topology.AutoProvision;

        if (!autoProvision)
        {
            return;
        }

        // ARM-based provisioning requires explicit resource coordinates.
        // When using connection strings (e.g. emulator), hubs are pre-created
        // externally, so skip provisioning silently.
        if (_transportConfiguration.SubscriptionId is null
            || _transportConfiguration.ResourceGroupName is null
            || _transportConfiguration.NamespaceName is null)
        {
            return;
        }

        var logger = context.Services.GetRequiredService<ILogger<EventHubProvisioner>>();
        var provisioner = EventHubProvisioner.Create(_transportConfiguration, ConnectionManager.ConnectionProvider, logger);

        foreach (var topic in _topology.Topics)
        {
            if (topic.AutoProvision ?? autoProvision)
            {
                await topic.ProvisionAsync(provisioner, cancellationToken);
            }
        }

        foreach (var subscription in _topology.Subscriptions)
        {
            if (subscription.AutoProvision ?? autoProvision)
            {
                await subscription.ProvisionAsync(provisioner, cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    protected override ReceiveEndpoint CreateReceiveEndpoint()
    {
        return new EventHubReceiveEndpoint(this);
    }

    /// <inheritdoc />
    protected override DispatchEndpoint CreateDispatchEndpoint()
    {
        return new EventHubDispatchEndpoint(this);
    }

    /// <inheritdoc />
    public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        OutboundRoute route)
    {
        EventHubDispatchEndpointConfiguration? configuration = null;

        if (route.Kind is OutboundRouteKind.Send or OutboundRouteKind.Publish)
        {
            var hubName = GetHubName(context, route);

            configuration = new EventHubDispatchEndpointConfiguration
            {
                HubName = hubName,
                Name = "h/" + hubName
            };
        }

        return configuration;
    }

    /// <inheritdoc />
    public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        Uri address)
    {
        EventHubDispatchEndpointConfiguration? configuration = null;

        var path = address.AbsolutePath.AsSpan();
        Span<Range> ranges = stackalloc Range[2];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

        // eventhub:///h/{hub-name}
        if (address.Scheme == Schema && address.Host is "" && segmentCount == 2)
        {
            var kind = path[ranges[0]];
            var name = path[ranges[1]];

            if (kind is "h")
            {
                configuration = new EventHubDispatchEndpointConfiguration
                {
                    HubName = new string(name),
                    Name = "h/" + new string(name)
                };
            }
        }

        // eventhub://{namespace}/h/{hub-name}
        if (configuration is null && _topology.Address.IsBaseOf(address) && segmentCount == 2)
        {
            var kind = path[ranges[0]];
            var name = path[ranges[1]];

            if (kind is "h")
            {
                configuration = new EventHubDispatchEndpointConfiguration
                {
                    HubName = new string(name),
                    Name = "h/" + new string(name)
                };
            }
        }

        // hub://hub-name, where the hub name is the URI host.
        if (configuration is null && address is { Scheme: "hub" })
        {
            var hubName = address.Host;
            configuration = new EventHubDispatchEndpointConfiguration
            {
                HubName = hubName,
                Name = "h/" + hubName
            };
        }

        return configuration;
    }

    /// <inheritdoc />
    public override ReceiveEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        InboundRoute route)
    {
        if (route.Kind is InboundRouteKind.Subscribe or InboundRouteKind.Send
            && route.MessageType is not null)
        {
            var hubName = GetHubName(context, route);
            var consumerGroup = context.Host.ServiceName is not null
                ? ToKebabCase(context.Host.ServiceName)
                : route.Kind is InboundRouteKind.Send
                    ? hubName
                    : "$Default";

            return new EventHubReceiveEndpointConfiguration
            {
                Name = hubName,
                HubName = hubName,
                ConsumerGroup = consumerGroup
            };
        }

        return null;
    }

    private static string GetHubName(IMessagingConfigurationContext context, OutboundRoute route)
        => route.Kind == OutboundRouteKind.Send
            ? context.Naming.GetSendEndpointName(route.MessageType.RuntimeType)
            : context.Naming.GetPublishEndpointName(route.MessageType.RuntimeType);

    private static string GetHubName(IMessagingConfigurationContext context, InboundRoute route)
        => route.Kind == InboundRouteKind.Send
            ? context.Naming.GetSendEndpointName(route.MessageType!.RuntimeType)
            : context.Naming.GetPublishEndpointName(route.MessageType!.RuntimeType);

    /// <inheritdoc />
    protected override void OnAfterDiscoverEndpoints(IMessagingSetupContext context)
    {
        ValidateExplicitDispatchRoutes();
        ValidateInboundRoutes(context);
    }

    private void ValidateExplicitDispatchRoutes()
    {
        var routeBindings =
            from endpoint in DispatchEndpoints.OfType<EventHubDispatchEndpoint>()
            from route in endpoint.Configuration.Routes
            where route.Kind is OutboundRouteKind.Send or OutboundRouteKind.Publish
            let hubName = endpoint.Configuration.HubName
            where hubName is not null
            group route.Kind by (hubName, route.RuntimeType) into bindings
            select bindings;

        foreach (var bindings in routeBindings)
        {
            var hasSend = bindings.Contains(OutboundRouteKind.Send);
            var hasPublish = bindings.Contains(OutboundRouteKind.Publish);
            if (hasSend && hasPublish)
            {
                throw ThrowHelper.TransportCannotShareSendAndPublishStream(
                    Name,
                    bindings.Key.RuntimeType.FullName ?? bindings.Key.RuntimeType.Name,
                    bindings.Key.hubName);
            }
        }
    }

    private void ValidateInboundRoutes(IMessagingSetupContext context)
    {
        var eventHubInboundRoutes =
            from route in context.Router.InboundRoutes
            where route is { MessageType: not null, Endpoint: EventHubReceiveEndpoint endpoint }
                && ReferenceEquals(endpoint.Transport, this)
            group route by route.MessageType!.RuntimeType into routeGroup
            select routeGroup;

        foreach (var routes in eventHubInboundRoutes)
        {
            var hasCommandRoute = routes.Any(route => route.Kind == InboundRouteKind.Send);
            var hasEventRoute = routes.Any(route => route.Kind == InboundRouteKind.Subscribe);
            if (hasCommandRoute && hasEventRoute)
            {
                throw ThrowHelper.EventHubMessageContractCannotBeCommandAndEvent(
                    routes.Key.FullName ?? routes.Key.Name);
            }

            if (!hasCommandRoute)
            {
                continue;
            }

            var owners =
                routes
                    .Where(route => route.Kind == InboundRouteKind.Send)
                    .Select(route => ((EventHubReceiveEndpoint)route.Endpoint!).Configuration)
                    .Select(configuration => (
                        HubName: configuration.HubName
                            ?? context.Naming.GetSendEndpointName(routes.Key),
                        ConsumerGroup: configuration.ConsumerGroup))
                    .Distinct()
                    .ToArray();

            if (owners.Length != 1 || owners[0].ConsumerGroup == "$Default")
            {
                throw ThrowHelper.EventHubCommandRequiresDedicatedOwner(
                    routes.Key.FullName ?? routes.Key.Name,
                    owners.Length == 1 ? owners[0].HubName : context.Naming.GetSendEndpointName(routes.Key));
            }
        }
    }

    /// <inheritdoc />
    public override bool TryGetDispatchEndpoint(
        Uri address,
        [NotNullWhen(true)] out DispatchEndpoint? endpoint)
    {
        if (address.Scheme == Schema)
        {
            foreach (var candidate in DispatchEndpoints)
            {
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
                if (candidate.Destination.Address == address)
                {
                    endpoint = candidate;
                    return true;
                }
            }
        }

        if (address is { Scheme: "hub" })
        {
            var hubName = address.Host;
            foreach (var candidate in DispatchEndpoints)
            {
                if (candidate.Destination is EventHubTopic topic && topic.Name == hubName)
                {
                    endpoint = candidate;
                    return true;
                }
            }
        }

        endpoint = null;
        return false;
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
                    "hub",
                    topic.Name,
                    topic.Address?.ToString(),
                    "inbound",
                    new Dictionary<string, object?>
                    {
                        ["partitionCount"] = topic.PartitionCount,
                        ["autoProvision"] = topic.AutoProvision ?? autoProvision
                    }));
        }

        foreach (var subscription in _topology.Subscriptions)
        {
            links.Add(
                new TopologyLinkDescription(
                    "consumer-group",
                    subscription.Address?.ToString(),
                    _topology.Topics
                        .FirstOrDefault(t => t.Name == subscription.TopicName)
                        ?.Address?.ToString(),
                    null,
                    "subscribe",
                    new Dictionary<string, object?>
                    {
                        ["consumerGroup"] = subscription.ConsumerGroup,
                        ["autoProvision"] = subscription.AutoProvision ?? autoProvision
                    }));
        }

        var topology = new TopologyDescription(_topology.Address.ToString(), entities, links);

        return new TransportDescription(
            _topology.Address.ToString(),
            Name,
            Schema,
            nameof(EventHubMessagingTransport),
            receiveEndpoints,
            dispatchEndpoints,
            topology,
            Capabilities);
    }

    /// <summary>
    /// Converts a PascalCase or camelCase string to kebab-case.
    /// Already-kebab strings pass through unchanged.
    /// </summary>
    private static string ToKebabCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Already kebab-case or snake_case, normalize only.
        if (input.Contains('-') || input.Contains('_'))
        {
            return input.ToLowerInvariant().Replace('_', '-');
        }

        var sb = new System.Text.StringBuilder(input.Length + 4);
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (char.IsUpper(c) && i > 0)
            {
                sb.Append('-');
            }

            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        foreach (var endpoint in DispatchEndpoints)
        {
            if (endpoint is EventHubDispatchEndpoint ehEndpoint)
            {
                await ehEndpoint.DisposeBatchDispatcherAsync();
            }
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (ConnectionManager is not null)
        {
            await ConnectionManager.DisposeAsync();
        }
    }
}
