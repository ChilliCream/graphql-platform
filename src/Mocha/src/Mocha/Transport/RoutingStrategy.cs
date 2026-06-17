using static Mocha.InboundRouteKind;

namespace Mocha;

/// <summary>
/// Base implementation for transport routing strategies. It owns the shared endpoint layout
/// discovery algorithm and leaves transport-specific configuration and resource layout to derived
/// strategies.
/// </summary>
public abstract class RoutingStrategy
{
    private MessagingTransport? _transport;

    /// <summary>
    /// Gets the transport that owns this strategy.
    /// </summary>
    protected MessagingTransport Transport
        => _transport ?? throw new InvalidOperationException("Routing strategy is not initialized.");

    internal void Initialize(MessagingTransport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);

        if (_transport is not null)
        {
            throw new InvalidOperationException("Routing strategy is already initialized.");
        }

        _transport = transport;

        OnInitialize(transport);
    }

    protected virtual void OnInitialize(MessagingTransport transport) { }

    public abstract DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        OutboundRoute route);

    public abstract DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        Uri address);

    public abstract ReceiveEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        InboundRoute route);

    public virtual void DiscoverEndpoints(IMessagingSetupContext context)
    {
        DiscoverReplyEndpoints(context);

        if (Transport.BindMode == MessagingBindMode.Implicit)
        {
            DiscoverImplicitEndpoints(context);
        }

        DiscoverOutboundEndpoints(context);
        DiscoverEndpointTopology(context);
    }

    public virtual void DiscoverTopology(
        IMessagingConfigurationContext context,
        ReceiveEndpoint endpoint,
        ReceiveEndpointConfiguration configuration)
    {
    }

    public virtual void DiscoverTopology(
        IMessagingConfigurationContext context,
        DispatchEndpoint endpoint,
        DispatchEndpointConfiguration configuration)
    {
    }

    protected virtual void DiscoverReplyEndpoints(IMessagingSetupContext context)
    {
        if (Transport.ReceiveEndpoints.FirstOrDefault(x => x.Kind == ReceiveEndpointKind.Reply) is null)
        {
            var replyConsumer = context.Consumers.OfType<ReplyConsumer>().FirstOrDefault();

            if (replyConsumer is null)
            {
                throw ThrowHelper.ReplyConsumerNotFound();
            }

            var route = new InboundRoute();
            var routeConfiguration = new InboundRouteConfiguration { Kind = Reply, Consumer = replyConsumer };

            route.Initialize(context, routeConfiguration);

            var endpointConfiguration = CreateEndpointConfiguration(context, route);
            if (endpointConfiguration is null)
            {
                throw ThrowHelper.EndpointConfigurationFailed();
            }

            var endpoint = Transport.AddEndpoint(context, endpointConfiguration);

            route.ConnectEndpoint(context, endpoint);
        }

        if (Transport.DispatchEndpoints.FirstOrDefault(x => x.Kind == DispatchEndpointKind.Reply) is null)
        {
            var uri = new UriBuilder
            {
                Host = "",
                Scheme = Transport.Schema,
                Path = "replies"
            }.Uri;

            var endpointConfiguration = CreateEndpointConfiguration(context, uri);
            if (endpointConfiguration is not null)
            {
                Transport.AddEndpoint(context, endpointConfiguration);
            }
        }

        Transport.ReplyDispatchEndpoint = Transport.DispatchEndpoints.FirstOrDefault(x => x.Kind == DispatchEndpointKind.Reply);
        Transport.ReplyReceiveEndpoint = Transport.ReceiveEndpoints.FirstOrDefault(x => x.Kind == ReceiveEndpointKind.Reply);
    }

    protected virtual void DiscoverImplicitEndpoints(IMessagingSetupContext context)
    {
        var claimedTypeEndpoints = new Dictionary<Type, List<ReceiveEndpoint>>();
        foreach (var route in context.Router.InboundRoutes)
        {
            if (route.Endpoint is { Transport: { } endpointTransport } endpoint
                && endpointTransport == Transport
                && route.MessageType is { RuntimeType: { } runtimeType })
            {
                if (!claimedTypeEndpoints.TryGetValue(runtimeType, out var endpoints))
                {
                    endpoints = [];
                    claimedTypeEndpoints[runtimeType] = endpoints;
                }

                if (!endpoints.Contains(endpoint))
                {
                    endpoints.Add(endpoint);
                }
            }
        }

        foreach (var route in context.Router.InboundRoutes)
        {
            if (route.Endpoint is null)
            {
                if (route.MessageType is { RuntimeType: { } runtimeType }
                    && claimedTypeEndpoints.TryGetValue(runtimeType, out var claimingEndpoints))
                {
                    foreach (var claimingEndpoint in claimingEndpoints)
                    {
                        context.BindRouteToEndpoint(route, claimingEndpoint);
                    }
                }
                else
                {
                    Transport.ConnectRoute(context, route);
                }
            }

            if (route.Endpoint?.Transport == Transport)
            {
                CreateMatchingOutboundRoute(context, route);
            }
        }
    }

    private void CreateMatchingOutboundRoute(IMessagingSetupContext context, InboundRoute route)
    {
        if (route.Kind is Send or Request or Subscribe)
        {
            var outboundRouteKind = route.Kind is Send or Request ? OutboundRouteKind.Send : OutboundRouteKind.Publish;

            var outboundRoute = context.Router.OutboundRoutes.FirstOrDefault(x =>
                x.Kind == outboundRouteKind && x.MessageType == route.MessageType
            );

            if (outboundRoute is null)
            {
                outboundRoute = new OutboundRoute();
                var outboundRouteConfiguration = new OutboundRouteConfiguration
                {
                    MessageType = route.MessageType,
                    Kind = outboundRouteKind
                };
                outboundRoute.Initialize(context, outboundRouteConfiguration);
            }

            if (outboundRoute.Endpoint is null
                && outboundRoute.Destination is null)
            {
                Transport.ConnectRoute(context, outboundRoute);
            }
        }
    }

    private void DiscoverOutboundEndpoints(IMessagingSetupContext context)
    {
        foreach (var route in context.Router.OutboundRoutes)
        {
            if (route.Endpoint is null
                && route.Destination is null
                && ShouldConnectOutboundRoute(context, route))
            {
                Transport.ConnectRoute(context, route);
            }
        }
    }

    private bool ShouldConnectOutboundRoute(IMessagingSetupContext context, OutboundRoute route)
    {
        foreach (var inboundRoute in context.Router.InboundRoutes)
        {
            if (inboundRoute.MessageType != route.MessageType)
            {
                continue;
            }

            var outboundRouteKind = inboundRoute.Kind is Send or Request
                ? OutboundRouteKind.Send
                : inboundRoute.Kind is Subscribe
                    ? OutboundRouteKind.Publish
                    : (OutboundRouteKind?)null;

            if (outboundRouteKind == route.Kind
                && inboundRoute.Endpoint is { Transport: { } owner })
            {
                return owner == Transport;
            }
        }

        foreach (var transport in context.Transports)
        {
            if (transport.IsDefaultTransport)
            {
                return transport == Transport;
            }
        }

        return context.Transports.Length == 0 || context.Transports[0] == Transport;
    }

    private void DiscoverEndpointTopology(IMessagingSetupContext context)
    {
        foreach (var endpoint in Transport.ReceiveEndpoints)
        {
            endpoint.DiscoverTopology(context);
        }

        foreach (var endpoint in Transport.DispatchEndpoints)
        {
            endpoint.DiscoverTopology(context);
        }
    }
}
