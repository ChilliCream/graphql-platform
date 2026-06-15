using System.Diagnostics;
using Mocha.Features;
using static Mocha.InboundRouteKind;

namespace Mocha;

public abstract partial class MessagingTransport
{
    public bool IsInitialized { get; private set; }

    internal void Initialize(IMessagingSetupContext context)
    {
        AssertUninitialized();

        OnBeforeInitialize(context);

        Configuration = CreateConfiguration(context);

        if (Configuration is null)
        {
            throw ThrowHelper.TransportConfigurationMissing();
        }

        Name = Configuration.Name ?? throw ThrowHelper.TransportNameRequired();
        Schema = Configuration.Schema ?? throw ThrowHelper.TransportSchemaRequired();
        Naming = context.Naming;
        Conventions = new ConventionRegistry(context.Conventions.Concat(Configuration.Conventions));
        Options = Configuration.Options;
        BindMode = Configuration.BindMode;
        IsDefaultTransport = Configuration.IsDefaultTransport;

        _features = Configuration.Features;
        var busMiddlewares = context.Features.GetRequired<MiddlewareFeature>();
        var transportMiddlewares = new MiddlewareFeature(
            [.. busMiddlewares.DispatchMiddlewares, .. Configuration.DispatchMiddlewares],
            [.. busMiddlewares.DispatchPipelineModifiers, .. Configuration.DispatchPipelineModifiers],
            [.. busMiddlewares.ReceiveMiddlewares, .. Configuration.ReceiveMiddlewares],
            [.. busMiddlewares.ReceivePipelineModifiers, .. Configuration.ReceivePipelineModifiers],
            [.. busMiddlewares.HandlerMiddlewares],
            [.. busMiddlewares.HandlerPipelineModifiers]);

        _features.Set(transportMiddlewares);

        foreach (var endpointConfiguration in Configuration.ReceiveEndpoints)
        {
            var endpoint = AddEndpoint(context, endpointConfiguration);

            //  TODO maybe we should move this to endpoint initialize - not sure yet
            foreach (var handlerType in endpointConfiguration.ConsumerIdentities)
            {
                var consumer = context.Consumers.FirstOrDefault(h => h.Identity == handlerType)
                    ?? throw new InvalidOperationException(
                        $"Handler type {handlerType.FullName} not found. Call AddHandler<{handlerType.Name}>() before configuring the endpoint {Configuration.Name}.");

                foreach (var route in context.Router.GetInboundByConsumer(consumer))
                {
                    BindRouteToEndpoint(context, route, endpoint);
                }
            }

            foreach (var messageRuntimeType in endpointConfiguration.ReceivedMessageTypes)
            {
                var matched = context.Router.InboundRoutes
                    .Where(r => r.Kind is Subscribe or Send or Request
                                && r.MessageType?.RuntimeType == messageRuntimeType)
                    .ToList();

                if (matched.Count == 0)
                {
                    throw ThrowHelper.NoHandlerForMessageType(messageRuntimeType, endpointConfiguration.Name);
                }

                foreach (var route in matched)
                {
                    BindRouteToEndpoint(context, route, endpoint);
                }
            }
        }

        foreach (var endpointConfiguration in Configuration.DispatchEndpoints)
        {
            var endpoint = AddEndpoint(context, endpointConfiguration);

            foreach (var (runtimeType, kind) in endpointConfiguration.Routes)
            {
                var route = context.Router.OutboundRoutes.FirstOrDefault(x =>
                    x.Kind == kind && x.MessageType.RuntimeType == runtimeType
                );

                // in case we have found a matching route that has no endpoint and no destination,
                // we need to connect it to the endpoint
                if (route is not null
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                    && route.Endpoint is null
                    && route.Destination is null)
                {
                    route.ConnectEndpoint(context, endpoint);
                }
                else if (route is null)
                {
                    route = new OutboundRoute();
                    var routeConfiguration = new OutboundRouteConfiguration
                    {
                        MessageType = context.Messages.GetOrAdd(context, runtimeType),
                        Kind = kind
                    };
                    route.Initialize(context, routeConfiguration);
                    route.ConnectEndpoint(context, endpoint);
                }
            }
        }

        // for each handler match the outbound route
        foreach (var route in context.Router.InboundRoutes)
        {
            if (BindMode == MessagingBindMode.Implicit
                && route.Endpoint is { Transport: { } transport } && transport == this)
            {
                transport.CreateMatchingOutboundRoute(context, route);
            }
        }

        MarkInitialized();

        OnAfterInitialized(context);
    }

    private static void BindRouteToEndpoint(
        IMessagingSetupContext context,
        InboundRoute route,
        ReceiveEndpoint endpoint)
    {
        if (route.Endpoint is null)
        {
            route.ConnectEndpoint(context, endpoint);
            return;
        }

        if (route.Endpoint == endpoint)
        {
            return;
        }

        // The route is bound to another endpoint, so fan it out by adding an equivalent route to
        // this endpoint. Skip when an equivalent route is already present so binding the same
        // message type or consumer across three or more endpoints stays idempotent.
        // Conditions are not part of the equivalence check, so two routes for the same consumer
        // and message type that differ only by condition will not both be fanned out.
        foreach (var existing in context.Router.GetInboundByEndpoint(endpoint))
        {
            if (existing.Consumer == route.Consumer
                && existing.Kind == route.Kind
                && existing.MessageType == route.MessageType)
            {
                return;
            }
        }

        var clone = new InboundRoute();
        clone.Initialize(context, new InboundRouteConfiguration
        {
            MessageType = route.MessageType,
            Consumer = route.Consumer,
            Kind = route.Kind,
            Condition = route.Condition
        });
        clone.ConnectEndpoint(context, endpoint);
    }

    protected virtual void OnBeforeInitialize(IMessagingSetupContext context) { }

    protected virtual void OnAfterInitialized(IMessagingSetupContext context) { }

    internal void DiscoverEndpoints(IMessagingSetupContext context)
    {
        AssertInitialized();

        OnBeforeDiscoverEndpoints(context);

        var router = context.Router;

        // discover reply receive endpoint
        if (ReceiveEndpoints.FirstOrDefault(x => x.Kind == ReceiveEndpointKind.Reply) is null)
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

            var endpoint = AddEndpoint(context, endpointConfiguration);

            route.ConnectEndpoint(context, endpoint);
        }

        // discover reply dispatch endpoint
        if (DispatchEndpoints.FirstOrDefault(x => x.Kind == DispatchEndpointKind.Reply) is null)
        {
            var uri = new UriBuilder
            {
                Host = "",
                Scheme = Schema,
                Path = "replies"
            }.Uri;

            var endpointConfiguration = CreateEndpointConfiguration(context, uri);
            if (endpointConfiguration is not null)
            {
                AddEndpoint(context, endpointConfiguration);
            }
        }

        // Discover receive endpoints
        if (BindMode == MessagingBindMode.Implicit)
        {
            // First find message types that are already bound to an endpoint on this transport.
            // Example: Queue("orders").Receives<OrderCreated>() makes "orders" the owner of
            // OrderCreated. If OrderCreatedHandlerA is already on "orders" and
            // OrderCreatedHandlerB is still unbound, HandlerB should go to "orders" too instead
            // of getting a second auto-created endpoint.
            var claimedTypeEndpoints = new Dictionary<Type, List<ReceiveEndpoint>>();
            foreach (var route in router.InboundRoutes)
            {
                if (route.Endpoint is { Transport: { } endpointTransport } endpoint
                    && endpointTransport == this
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

            // Now connect the remaining routes. Claimed types go to their owner endpoint, so all
            // handlers for the same message type stay together. Unclaimed types are handled by
            // normal implicit discovery, which creates the default endpoint for the route.
            // The owner map is built first so route order does not change the result.
            foreach (var route in router.InboundRoutes)
            {
                if (route.Endpoint is null)
                {
                    if (route.MessageType is { RuntimeType: { } runtimeType }
                        && claimedTypeEndpoints.TryGetValue(runtimeType, out var claimingEndpoints))
                    {
                        foreach (var claimingEndpoint in claimingEndpoints)
                        {
                            BindRouteToEndpoint(context, route, claimingEndpoint);
                        }
                    }
                    else
                    {
                        ConnectRoute(context, route);
                    }
                }

                CreateMatchingOutboundRoute(context, route);
            }
        }

        // discover outbound routes
        // TODO i am not sure if this is correct.
        foreach (var route in router.OutboundRoutes)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (route.Endpoint is null
                && route.Destination is null)
            {
                ConnectRoute(context, route);
            }
        }

        ReplyDispatchEndpoint = DispatchEndpoints.FirstOrDefault(x => x.Kind == DispatchEndpointKind.Reply);
        ReplyReceiveEndpoint = ReceiveEndpoints.FirstOrDefault(x => x.Kind == ReceiveEndpointKind.Reply);
        // Request/reply depends on both endpoints: one to receive inbound replies and one to emit them.

        foreach (var endpoint in _receiveEndpoints)
        {
            endpoint.DiscoverTopology(context);
        }

        foreach (var endpoint in _dispatchEndpoints)
        {
            endpoint.DiscoverTopology(context);
        }

        OnAfterDiscoverEndpoints(context);
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

            // a route with an explicit destination is connected later through the
            // destination loop, so we must not claim it here.
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (outboundRoute.Endpoint is null
                && outboundRoute.Destination is null)
            {
                ConnectRoute(context, outboundRoute);
            }
        }
    }

    protected virtual void OnBeforeDiscoverEndpoints(IMessagingSetupContext context) { }

    protected virtual void OnAfterDiscoverEndpoints(IMessagingSetupContext context) { }

    internal void Complete(IMessagingSetupContext context)
    {
        AssertInitialized();

        foreach (var endpoint in _receiveEndpoints)
        {
            if (!endpoint.IsCompleted)
            {
                endpoint.Complete(context);
            }
        }

        foreach (var endpoint in _dispatchEndpoints)
        {
            if (!endpoint.IsCompleted)
            {
                endpoint.Complete(context);
            }
        }
    }

    internal void Finalize(IMessagingSetupContext _)
    {
        _features = _features?.ToReadOnly() ?? FeatureCollection.Empty;
        Configuration = null!;
    }

    private void AssertUninitialized()
    {
        Debug.Assert(!IsInitialized, "The type must be uninitialized.");

        if (IsInitialized)
        {
            throw new InvalidOperationException();
        }
    }

    protected void AssertInitialized()
    {
        Debug.Assert(IsInitialized, "The type must be initialized.");

        if (!IsInitialized)
        {
            throw new InvalidOperationException();
        }
    }

    public void MarkInitialized()
    {
        IsInitialized = true;
    }
}
