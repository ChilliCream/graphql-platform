using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Represents an inbound message route that binds a message type to a consumer on a receive endpoint.
/// </summary>
public sealed class InboundRoute
{
    /// <summary>
    /// Gets a value indicating whether the route has been initialized with its message type, consumer, and kind.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Consumer))]
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the route has been fully completed with an endpoint connection.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Endpoint), nameof(Consumer))]
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// Gets the message type that this route handles, or <c>null</c> for reply routes.
    /// </summary>
    public MessageType? MessageType { get; private set; }

    /// <summary>
    /// Gets the consumer that handles messages arriving on this route.
    /// </summary>
    public Consumer? Consumer { get; private set; }

    /// <summary>
    /// Gets the kind of inbound route (subscribe, send, request, or reply).
    /// </summary>
    public InboundRouteKind Kind { get; private set; }

    /// <summary>
    /// Gets the receive endpoint that this route is connected to, or <c>null</c> if not yet connected.
    /// </summary>
    public ReceiveEndpoint? Endpoint { get; private set; }

    /// <summary>
    /// Initializes the inbound route from configuration, resolving the message type and consumer.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="configuration">The inbound route configuration.</param>
    public void Initialize(IMessagingConfigurationContext context, InboundRouteConfiguration configuration)
    {
        AssertNotInitialized();

        context.Conventions.Configure(context, configuration);

        if (configuration.MessageType is not null)
        {
            MessageType = configuration.MessageType;
        }
        else if (configuration.MessageRuntimeType is not null)
        {
            MessageType = context.Messages.GetOrAdd(context, configuration.MessageRuntimeType);
        }
        else if (configuration.Kind != InboundRouteKind.Reply)
        {
            throw new InvalidOperationException("Route requires a message type");
        }

        Consumer = configuration.Consumer ?? throw new InvalidOperationException("Route requires a consumer");
        Kind = configuration.Kind;

        if (configuration.ResponseRuntimeType is not null)
        {
            context.Messages.GetOrAdd(context, configuration.ResponseRuntimeType);
        }

        MarkInitialized();
    }

    /// <summary>
    /// Connects this route to the specified receive endpoint and registers it with the router.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="endpoint">The receive endpoint to connect to.</param>
    public void ConnectEndpoint(IMessagingConfigurationContext context, ReceiveEndpoint endpoint)
    {
        AssertInitialized();
        AssertNotCompleted();

        Endpoint = endpoint;
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
            throw new InvalidOperationException("Endpoint is not connected");
        }

        MarkCompleted();
    }

    private void AssertNotInitialized()
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException("Route must not be initialized");
        }
    }

    private void AssertInitialized()
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException("Rout must be initialized");
        }
    }

    private void AssertNotCompleted()
    {
        if (IsCompleted)
        {
            throw new InvalidOperationException("Route must not be completed");
        }
    }

    private void MarkInitialized()
    {
        IsInitialized = true;
    }

    /// <summary>
    /// Creates a description of this inbound route for visualization and diagnostic purposes.
    /// </summary>
    /// <returns>An <see cref="InboundRouteDescription"/> representing this route.</returns>
    public InboundRouteDescription Describe()
    {
        return new InboundRouteDescription(
            Kind,
            MessageType?.Identity,
            Consumer?.Name,
            Endpoint is not null
                ? new EndpointReferenceDescription(Endpoint.Name, Endpoint.Address?.ToString(), Endpoint.Transport.Name)
                : null);
    }

    private void MarkCompleted()
    {
        IsCompleted = true;
    }
}
