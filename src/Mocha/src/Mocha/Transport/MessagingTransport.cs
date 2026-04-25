using System.Diagnostics.CodeAnalysis;
using Mocha.Resources;

namespace Mocha;

/// <summary>
/// Base class for all messaging transports, managing the lifecycle of receive and dispatch endpoints,
/// topology, and connection to the underlying messaging infrastructure (e.g., RabbitMQ, in-memory).
/// </summary>
/// <remarks>
/// Transport implementations must override abstract members to provide endpoint creation, configuration,
/// and topology details. The transport must be initialized before it can be started. Starting a transport
/// activates all its receive endpoints; stopping deactivates them. Dispatch endpoints are created lazily
/// as outbound routes are connected.
/// </remarks>
public abstract partial class MessagingTransport : IAsyncDisposable, IFeatureProvider
{
    /// <summary>
    /// The human-readable name of this transport instance, typically set during configuration.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The URI scheme (e.g., "rabbitmq", "memory") used to match addresses to this transport.
    /// </summary>
    public string Schema { get; protected set; } = null!;

    /// <summary>
    /// Read-only transport-level options such as concurrency limits and prefetch settings.
    /// </summary>
    public IReadOnlyTransportOptions Options { get; private set; } = null!;

    private readonly HashSet<ReceiveEndpoint> _receiveEndpoints = [];
    private readonly HashSet<DispatchEndpoint> _dispatchEndpoints = [];

    /// <summary>
    /// The set of receive endpoints registered on this transport, each consuming messages from a source.
    /// </summary>
    public IReadOnlySet<ReceiveEndpoint> ReceiveEndpoints => _receiveEndpoints;

    /// <summary>
    /// The set of dispatch endpoints registered on this transport, each sending messages to a destination.
    /// </summary>
    public IReadOnlySet<DispatchEndpoint> DispatchEndpoints => _dispatchEndpoints;

    /// <summary>
    /// The receive endpoint used to accept reply messages for request/response flows, or
    /// <see langword="null"/> if the transport does not support replies.
    /// </summary>
    public ReceiveEndpoint? ReplyReceiveEndpoint { get; protected set; }

    /// <summary>
    /// The dispatch endpoint used to send reply messages back to requestors, or
    /// <see langword="null"/> if the transport does not support replies.
    /// </summary>
    public DispatchEndpoint? ReplyDispatchEndpoint { get; protected set; }

    private IFeatureCollection? _features;

    /// <summary>
    /// The feature collection for this transport, providing access to transport-scoped features.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before the transport is initialized.</exception>
    public IFeatureCollection Features
        => _features ?? throw ThrowHelper.FeaturesNotInitialized();

    /// <summary>
    /// The messaging topology that describes the transport's addressing structure (exchanges, queues, topics).
    /// </summary>
    public abstract MessagingTopology Topology { get; }

    /// <summary>
    /// Naming conventions used to derive endpoint, queue, and exchange names from message types and consumers.
    /// </summary>
    public IBusNamingConventions Naming { get; protected set; } = null!;

    /// <summary>
    /// The configuration object that was used to initialize this transport, containing middleware pipelines,
    /// endpoint definitions, and transport-specific settings.
    /// </summary>
    protected internal MessagingTransportConfiguration Configuration { get; protected set; } = null!;

    /// <summary>
    /// The convention registry scoped to this transport, applied during routing and endpoint configuration.
    /// </summary>
    public IConventionRegistry Conventions { get; protected set; } = null!;

    /// <summary>
    /// Produces a structural description of this transport including its endpoints, topology entities,
    /// and inbound/outbound resource bindings, suitable for visualization or diagnostics.
    /// </summary>
    /// <returns>A <see cref="TransportDescription"/> capturing the current transport topology and endpoint state.</returns>
    public virtual TransportDescription Describe()
    {
        var receiveEndpoints = ReceiveEndpoints.Select(e => e.Describe()).ToList();

        var dispatchEndpoints = DispatchEndpoints.Select(e => e.Describe()).ToList();

        var entities = new List<TopologyEntityDescription>();
        var outboundResources = new HashSet<TopologyResource>();
        var inboundResources = new HashSet<TopologyResource>();

        foreach (var endpoint in ReceiveEndpoints)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (endpoint.Source is not null)
            {
                outboundResources.Add(endpoint.Source);
            }
        }

        foreach (var endpoint in DispatchEndpoints)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (endpoint.Destination is not null)
            {
                inboundResources.Add(endpoint.Destination);
            }
        }

        foreach (var resource in outboundResources)
        {
            entities.Add(
                new TopologyEntityDescription(
                    resource.GetType().Name.ToLowerInvariant(),
                    null,
                    resource.Address?.ToString(),
                    "outbound",
                    null));
        }

        foreach (var resource in inboundResources)
        {
            if (outboundResources.Contains(resource))
            {
                continue;
            }

            entities.Add(
                new TopologyEntityDescription(
                    resource.GetType().Name.ToLowerInvariant(),
                    null,
                    resource.Address?.ToString(),
                    "inbound",
                    null));
        }

        var topology = new TopologyDescription(Topology.Address.ToString(), entities, []);

        return new TransportDescription(
            Topology.Address.ToString(),
            Name,
            Schema,
            GetType().Name,
            receiveEndpoints,
            dispatchEndpoints,
            topology);
    }

    /// <summary>
    /// Contributes <see cref="MochaResource"/> instances representing this transport, its
    /// receive/dispatch endpoints, and its topology entities to the supplied collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default implementation contributes nothing. Transports that want to expose richer
    /// topology semantics (durability flags, exchange types, bindings, …) override this method
    /// and append their own <see cref="MochaResource"/> subclasses directly. When a transport
    /// does not override this method, the message-bus resource source falls back to a generic
    /// projection of <see cref="Describe"/> so consumers always see a transport, its endpoints,
    /// and its topology entities.
    /// </para>
    /// <para>
    /// Implementations must append to <paramref name="resources"/>; they must not clear
    /// or otherwise mutate entries previously added by other contributors.
    /// </para>
    /// </remarks>
    /// <param name="resources">The collection to append contributed resources to.</param>
    public virtual void ContributeMochaResources(ICollection<MochaResource> resources)
    {
        ArgumentNullException.ThrowIfNull(resources);
    }

    /// <summary>
    /// Attempts to retrieve an existing dispatch endpoint for the specified address.
    /// </summary>
    /// <param name="address">The destination URI to look up.</param>
    /// <param name="endpoint">
    /// When this method returns <see langword="true"/>, contains the dispatch endpoint for
    /// <paramref name="address"/>; otherwise <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if a dispatch endpoint exists for the address; otherwise <see langword="false"/>.</returns>
    public abstract bool TryGetDispatchEndpoint(Uri address, [NotNullWhen(true)] out DispatchEndpoint? endpoint);

    /// <summary>
    /// Indicates whether this transport has been started and its receive endpoints are actively consuming messages.
    /// </summary>
    public bool IsStarted { get; private set; }

    /// <summary>
    /// Starts the transport by invoking pre-start hooks and activating all receive endpoints.
    /// </summary>
    /// <param name="context">The runtime context providing access to services and configuration.</param>
    /// <param name="cancellationToken">A token to cancel the startup sequence.</param>
    /// <exception cref="InvalidOperationException">Thrown if the transport is already started or not initialized.</exception>
    public async ValueTask StartAsync(IMessagingRuntimeContext context, CancellationToken cancellationToken)
    {
        AssertInitialized();
        if (IsStarted)
        {
            throw ThrowHelper.TransportAlreadyStarted();
        }

        await OnBeforeStartAsync(context, cancellationToken);

        foreach (var endpoint in ReceiveEndpoints)
        {
            await endpoint.StartAsync(context, cancellationToken);
        }

        IsStarted = true;
    }

    /// <summary>
    /// Stops the transport by invoking pre-stop hooks and deactivating all receive endpoints.
    /// </summary>
    /// <param name="context">The runtime context providing access to services and configuration.</param>
    /// <param name="cancellationToken">A token to cancel the shutdown sequence.</param>
    /// <exception cref="InvalidOperationException">Thrown if the transport is not currently started.</exception>
    public async ValueTask StopAsync(IMessagingRuntimeContext context, CancellationToken cancellationToken)
    {
        if (!IsStarted)
        {
            throw ThrowHelper.TransportNotStarted();
        }

        await OnBeforeStopAsync(cancellationToken);
        foreach (var endpoint in ReceiveEndpoints)
        {
            await endpoint.StopAsync(context, cancellationToken);
        }

        IsStarted = false;
    }

    /// <summary>
    /// Called before receive endpoints are started, allowing derived transports to perform
    /// connection setup or topology declaration.
    /// </summary>
    /// <param name="context">The configuration context for the current startup phase.</param>
    /// <param name="cancellationToken">A token to cancel the pre-start operation.</param>
    protected virtual ValueTask OnBeforeStartAsync(
        IMessagingConfigurationContext context,
        CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    /// <summary>
    /// Called before receive endpoints are stopped, allowing derived transports to perform
    /// graceful connection teardown or resource cleanup.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the pre-stop operation.</param>
    protected virtual ValueTask OnBeforeStopAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    /// <summary>
    /// Creates the transport-specific configuration from the setup context during the bus build phase.
    /// </summary>
    /// <param name="context">The setup context providing access to registered options and services.</param>
    /// <returns>A <see cref="MessagingTransportConfiguration"/> describing this transport's endpoints and pipelines.</returns>
    protected abstract MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext context);

    /// <summary>
    /// Connects an outbound route to a dispatch endpoint, creating the endpoint if one does not already
    /// exist for the route's resolved configuration name.
    /// </summary>
    /// <param name="context">The configuration context used to initialize new endpoints.</param>
    /// <param name="route">The outbound route to bind to a dispatch endpoint.</param>
    /// <returns>The dispatch endpoint that the route was connected to.</returns>
    /// <exception cref="InvalidOperationException">Thrown when endpoint configuration cannot be created for the route.</exception>
    public DispatchEndpoint ConnectRoute(IMessagingConfigurationContext context, OutboundRoute route)
    {
        if (CreateEndpointConfiguration(context, route) is not { } configuration)
        {
            throw ThrowHelper.EndpointConfigurationFailed();
        }

        var endpoint =
            DispatchEndpoints.FirstOrDefault(x => x.Name == configuration.Name) ?? AddEndpoint(context, configuration);

        route.ConnectEndpoint(context, endpoint);

        return endpoint;
    }

    /// <summary>
    /// Connects an inbound route to a receive endpoint, creating the endpoint if one does not already
    /// exist for the route's resolved configuration name.
    /// </summary>
    /// <param name="context">The configuration context used to initialize new endpoints.</param>
    /// <param name="route">The inbound route to bind to a receive endpoint.</param>
    /// <returns>The receive endpoint that the route was connected to.</returns>
    /// <exception cref="InvalidOperationException">Thrown when endpoint configuration cannot be created for the route.</exception>
    public ReceiveEndpoint ConnectRoute(IMessagingConfigurationContext context, InboundRoute route)
    {
        if (CreateEndpointConfiguration(context, route) is not { } configuration)
        {
            throw ThrowHelper.EndpointConfigurationFailed();
        }

        var endpoint =
            ReceiveEndpoints.FirstOrDefault(x => x.Name == configuration.Name) ?? AddEndpoint(context, configuration);

        route.ConnectEndpoint(context, endpoint);

        return endpoint;
    }

    /// <summary>
    /// Creates and registers a new dispatch endpoint from the given configuration.
    /// </summary>
    /// <param name="context">The configuration context used to initialize the endpoint.</param>
    /// <param name="configuration">The dispatch endpoint configuration specifying name, destination, and pipeline.</param>
    /// <returns>The newly created and registered dispatch endpoint.</returns>
    public DispatchEndpoint AddEndpoint(
        IMessagingConfigurationContext context,
        DispatchEndpointConfiguration configuration)
    {
        var endpoint = CreateDispatchEndpoint();

        endpoint.Initialize(context, configuration);

        _dispatchEndpoints.Add(endpoint);

        return endpoint;
    }

    /// <summary>
    /// Creates and registers a new receive endpoint from the given configuration.
    /// </summary>
    /// <param name="context">The configuration context used to initialize the endpoint.</param>
    /// <param name="configuration">The receive endpoint configuration specifying name, source, consumers, and pipeline.</param>
    /// <returns>The newly created and registered receive endpoint.</returns>
    public ReceiveEndpoint AddEndpoint(
        IMessagingConfigurationContext context,
        ReceiveEndpointConfiguration configuration)
    {
        var endpoint = CreateReceiveEndpoint();

        endpoint.Initialize(context, configuration);

        _receiveEndpoints.Add(endpoint);

        return endpoint;
    }

    // TODO can we consolidate this as EndpointId?
    /// <summary>
    /// Creates the dispatch endpoint configuration for the given outbound route, or returns
    /// <see langword="null"/> if the route cannot be mapped by this transport.
    /// </summary>
    /// <param name="context">The configuration context for endpoint creation.</param>
    /// <param name="route">The outbound route describing the message type and routing kind.</param>
    /// <returns>
    /// A <see cref="DispatchEndpointConfiguration"/> if the route can be served by this transport;
    /// otherwise <see langword="null"/>.
    /// </returns>
    public abstract DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        OutboundRoute route);

    /// <summary>
    /// Creates the dispatch endpoint configuration for the given destination address, or returns
    /// <see langword="null"/> if the address cannot be mapped by this transport.
    /// </summary>
    /// <param name="context">The configuration context for endpoint creation.</param>
    /// <param name="address">The destination URI to create an endpoint for.</param>
    /// <returns>
    /// A <see cref="DispatchEndpointConfiguration"/> if the address can be served by this transport;
    /// otherwise <see langword="null"/>.
    /// </returns>
    public abstract DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        Uri address);

    /// <summary>
    /// Creates the receive endpoint configuration for the given inbound route, or returns
    /// <see langword="null"/> if the route cannot be mapped by this transport.
    /// </summary>
    /// <param name="context">The configuration context for endpoint creation.</param>
    /// <param name="route">The inbound route describing the consumer bindings and source.</param>
    /// <returns>
    /// A <see cref="ReceiveEndpointConfiguration"/> if the route can be served by this transport;
    /// otherwise <see langword="null"/>.
    /// </returns>
    public abstract ReceiveEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        InboundRoute route);

    /// <summary>
    /// Factory method to create a transport-specific receive endpoint instance.
    /// </summary>
    /// <returns>A new, uninitialized <see cref="ReceiveEndpoint"/> appropriate for this transport.</returns>
    protected abstract ReceiveEndpoint CreateReceiveEndpoint();

    /// <summary>
    /// Factory method to create a transport-specific dispatch endpoint instance.
    /// </summary>
    /// <returns>A new, uninitialized <see cref="DispatchEndpoint"/> appropriate for this transport.</returns>
    protected abstract DispatchEndpoint CreateDispatchEndpoint();

    /// <inheritdoc />
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
