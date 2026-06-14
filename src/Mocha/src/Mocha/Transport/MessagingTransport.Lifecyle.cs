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
        ConsumerBindingMode = Configuration.ConsumerBindingMode;
        AutoBind = Configuration.AutoBind ?? true;
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
                // Reject Receives<T> when T is a reply type. Two detection paths cover both cases:
                // (1) a route with Kind = Reply exists for the type (saga OnReply<T>),
                // (2) the type is the response of a registered IEventRequest<T> (request-reply response).
                var isReplyRouteKind = context.Router.InboundRoutes
                    .Any(r => r.Kind == InboundRouteKind.Reply
                              && r.MessageType?.RuntimeType == messageRuntimeType);

                var isResponseRegistration = context.Messages.MessageTypes
                    .Any(mt => mt.RuntimeType.GetInterfaces()
                        .Any(i => i.IsGenericType
                                  && i.GetGenericTypeDefinition() == typeof(IEventRequest<>)
                                  && i.GetGenericArguments()[0] == messageRuntimeType));

                if (isReplyRouteKind || isResponseRegistration)
                {
                    throw ThrowHelper.ReceivesReplyType(messageRuntimeType.Name);
                }

                var matched = context.Router.InboundRoutes
                    .Where(r => r.Kind is Subscribe or Send or Request
                                && r.MessageType?.RuntimeType == messageRuntimeType)
                    .ToList();

                if (matched.Count == 0)
                {
                    // A type with at least one per-type BindFrom is exempt: the explicit binding
                    // routes messages into this queue from an external source, so no inbound route
                    // needs to exist in the router for the type to be consumed.
                    var hasBindFrom =
                        endpointConfiguration.TypeBinds.TryGetValue(messageRuntimeType, out var intent)
                        && intent.BindFroms.Count > 0;

                    if (!hasBindFrom)
                    {
                        throw ThrowHelper.NoHandlerForMessageType(messageRuntimeType, endpointConfiguration.Name);
                    }

                    continue;
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
            if (ConsumerBindingMode == ConsumerBindingMode.Implicit
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
        if (ConsumerBindingMode == ConsumerBindingMode.Implicit)
        {
            // A message type is claimed when a configured receive endpoint names it via
            // Handler/Consumer/Receives, in which case at least one route for the type is already
            // connected to that endpoint on this transport. Implicit discovery connects only unclaimed
            // routes onto framework-created endpoints; a claimed type binds its remaining routes to the
            // claiming endpoint instead, so the type converges on a single endpoint. The claim map is
            // computed up front so the outcome does not depend on the order routes are visited in.
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
