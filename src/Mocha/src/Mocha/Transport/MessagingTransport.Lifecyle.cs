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
        Routing = Configuration.RoutingStrategyFactory?.Invoke(context.Services)
            ?? throw ThrowHelper.TransportRoutingStrategyRequired();
        Routing.Initialize(this);

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
                    context.BindRouteToEndpoint(route, endpoint);
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
                    context.BindRouteToEndpoint(route, endpoint);
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

        MarkInitialized();

        OnAfterInitialized(context);
    }

    protected virtual void OnBeforeInitialize(IMessagingSetupContext context) { }

    protected virtual void OnAfterInitialized(IMessagingSetupContext context) { }

    internal void DiscoverEndpoints(IMessagingSetupContext context)
    {
        AssertInitialized();

        OnBeforeDiscoverEndpoints(context);

        Routing.DiscoverEndpoints(context);

        OnAfterDiscoverEndpoints(context);
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
