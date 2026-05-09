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
                var consumer = context.Consumers.FirstOrDefault(h => h.Identity == handlerType);

                if (consumer is not null)
                {
                    var routes = context.Router.GetInboundByConsumer(consumer);
                    var applied = false;
                    foreach (var route in routes)
                    {
                        ValidateInboundRouteCapability(route, endpointConfiguration.Name);

                        if (route.Endpoint is null)
                        {
                            route.ConnectEndpoint(context, endpoint);
                            applied = true;
                        }
                    }

                    // in this case the user has explicilty mapped this consumer to multiple
                    // endpoints and we are currently missing an additional route
                    if (!applied)
                    {
                        var route = new InboundRoute();
                        var configuration = new InboundRouteConfiguration
                        {
                            MessageType = route.MessageType,
                            Consumer = consumer
                        };
                        route.Initialize(context, configuration);
                        ValidateInboundRouteCapability(route, endpointConfiguration.Name);
                        route.ConnectEndpoint(context, endpoint);
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Handler type {handlerType.FullName} not found for endpoint {Configuration.Name}");
                }
            }
        }

        foreach (var endpointConfiguration in Configuration.DispatchEndpoints)
        {
            var endpoint = AddEndpoint(context, endpointConfiguration);

            foreach (var (runtimeType, kind) in endpointConfiguration.Routes)
            {
                ValidateOutboundRouteKindCapability(kind, runtimeType, endpointConfiguration.Name);

                var route = context.Router.OutboundRoutes.FirstOrDefault(x =>
                    x.Kind == kind && x.MessageType.RuntimeType == runtimeType
                );

                // in case we have found a matching route that has no endpoint and no destination,
                // we need to connect it to the endpoint
                if (route is not null
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                    && route.Endpoint is null
                    && route.Destination is not null)
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
            if (route.Endpoint is { Transport: { } transport } && transport == this)
            {
                transport.CreateMatchingOutboundRoute(context, route);
            }
        }

        MarkInitialized();

        OnAfterInitialized(context);
    }

    protected virtual void OnBeforeInitialize(IMessagingSetupContext context) { }

    protected virtual void OnAfterInitialized(IMessagingSetupContext context) { }

    internal void DiscoverEndpoints(IMessagingSetupContext context)
    {
        AssertInitialized();

        OnBeforeDiscoverEndpoints(context);

        var router = context.Router;
        var supportsRequestReply = HasCapability(MessagingTransportCapabilities.RequestReply);

        // discover reply receive endpoint
        if (supportsRequestReply
            && ReceiveEndpoints.FirstOrDefault(x => x.Kind == ReceiveEndpointKind.Reply) is null)
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
        if (supportsRequestReply
            && DispatchEndpoints.FirstOrDefault(x => x.Kind == DispatchEndpointKind.Reply) is null)
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
        if (Configuration.ConsumerBindingMode == ConsumerBindingMode.Implicit)
        {
            foreach (var route in router.InboundRoutes)
            {
                if (route.Endpoint is null)
                {
                    TryConnectRoute(context, route, out _);
                }

                if (route.Endpoint?.Transport == this)
                {
                    CreateMatchingOutboundRoute(context, route);
                }
            }
        }

        // discover outbound routes
        // TODO i am not sure if this is correct.
        foreach (var route in router.OutboundRoutes)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (route.Endpoint is null)
            {
                TryConnectRoute(context, route, out _);
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
            if (!HasCapability(MessagingTransportCapabilityMap.GetRequiredCapability(route.Kind)))
            {
                return;
            }

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

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (outboundRoute.Endpoint is null)
            {
                var outboundEndpoint = ConnectRoute(context, outboundRoute);

                outboundRoute.ConnectEndpoint(context, outboundEndpoint);
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

    private void ValidateInboundRouteCapability(InboundRoute route, string? endpointName)
    {
        var required = MessagingTransportCapabilityMap.GetRequiredCapability(route.Kind);

        if (HasCapability(required))
        {
            return;
        }

        throw ThrowHelper.TransportLacksCapabilityForInboundRoute(
            Name,
            route.Kind,
            required,
            route.MessageType?.Identity,
            $"Bind this route to a transport that supports the '{required}' capability"
            + (endpointName is not null ? $" instead of endpoint '{endpointName}'." : "."));
    }

    private void ValidateOutboundRouteKindCapability(
        OutboundRouteKind kind,
        Type runtimeType,
        string? endpointName)
    {
        var required = MessagingTransportCapabilityMap.GetRequiredCapability(kind);

        if (HasCapability(required))
        {
            return;
        }

        throw ThrowHelper.TransportLacksCapabilityForOutboundRoute(
            Name,
            kind,
            required,
            runtimeType.FullName,
            $"Bind this route to a transport that supports the '{required}' capability"
            + (endpointName is not null ? $" instead of endpoint '{endpointName}'." : "."));
    }
}
