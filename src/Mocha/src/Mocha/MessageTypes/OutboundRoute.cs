namespace Mocha;

/// <summary>
/// Represents an outbound message route that connects a message type to a dispatch endpoint for sending or publishing.
/// </summary>
public sealed class OutboundRoute
{
    /// <summary>
    /// Gets a value indicating whether the route has been initialized with its message type and kind.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the route has been fully completed with an endpoint connection.
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// Gets the kind of outbound route (send or publish).
    /// </summary>
    public OutboundRouteKind Kind { get; private set; }

    /// <summary>
    /// Gets the message type that this route handles.
    /// </summary>
    public MessageType MessageType { get; private set; } = null!;

    /// <summary>
    /// Gets the destination URI for this route, or <c>null</c> if not yet resolved.
    /// </summary>
    public Uri? Destination { get; private set; }

    /// <summary>
    /// Gets the dispatch endpoint that this route is connected to.
    /// </summary>
    public DispatchEndpoint Endpoint { get; private set; } = null!;

    /// <summary>
    /// Initializes the outbound route from configuration, resolving the message type.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="configuration">The outbound route configuration.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configuration does not specify a message type or runtime type.
    /// </exception>
    public void Initialize(IMessagingConfigurationContext context, OutboundRouteConfiguration configuration)
    {
        AssertNotInitialized();

        Kind = configuration.Kind;
        if (configuration.MessageType is not null)
        {
            MessageType = configuration.MessageType;
        }
        else if (configuration.RuntimeType is not null)
        {
            MessageType = context.Messages.GetOrAdd(context, configuration.RuntimeType);
        }
        else
        {
            throw ThrowHelper.RouteRequiresMessageType();
        }

        Destination = configuration.Destination;

        MarkInitialized();
    }

    /// <summary>
    /// Connects this route to the specified dispatch endpoint and updates the router.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="endpoint">The dispatch endpoint to connect to.</param>
    public void ConnectEndpoint(IMessagingConfigurationContext context, DispatchEndpoint endpoint)
    {
        AssertInitialized();
        AssertNotCompleted();

        Endpoint = endpoint;
        Destination ??= Endpoint.Address;
        context.Router.AddOrUpdate(this);
    }

    /// <summary>
    /// Completes the route initialization, verifying that an endpoint has been connected.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    public void Complete(IMessagingConfigurationContext context)
    {
        AssertInitialized();
        AssertNotCompleted();

        if (Endpoint is null)
        {
            throw ThrowHelper.RouteEndpointNotConnected();
        }

        Destination ??= Endpoint.Address;
        context.Router.AddOrUpdate(this);

        MarkCompleted();
    }

    private void AssertNotInitialized()
    {
        if (IsInitialized)
        {
            throw ThrowHelper.RouteMustNotBeInitialized();
        }
    }

    private void AssertInitialized()
    {
        if (!IsInitialized)
        {
            throw ThrowHelper.RouteMustBeInitialized();
        }
    }

    private void AssertNotCompleted()
    {
        if (IsCompleted)
        {
            throw ThrowHelper.RouteMustNotBeCompleted();
        }
    }

    private void MarkInitialized()
    {
        IsInitialized = true;
    }

    /// <summary>
    /// Creates a description of this outbound route for visualization and diagnostic purposes.
    /// </summary>
    /// <returns>An <see cref="OutboundRouteDescription"/> representing this route.</returns>
    public OutboundRouteDescription Describe()
    {
        return new OutboundRouteDescription(
            Kind,
            MessageType.Identity,
            Destination?.ToString(),
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            Endpoint is not null
                ? new EndpointReferenceDescription(Endpoint.Name, Endpoint.Address?.ToString(), Endpoint.Transport.Name)
                : null);
    }

    private void MarkCompleted()
    {
        IsCompleted = true;
    }
}
