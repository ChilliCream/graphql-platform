namespace Mocha;

/// <summary>
/// Provides context for constructing a receive middleware pipeline, including the service provider, endpoint, and transport.
/// </summary>
public class ReceiveMiddlewareFactoryContext
{
    /// <summary>
    /// Gets the service provider for resolving dependencies.
    /// </summary>
    public required IServiceProvider Services { get; init; }

    /// <summary>
    /// Gets the receive endpoint that this middleware pipeline is being built for.
    /// </summary>
    public required ReceiveEndpoint Endpoint { get; init; }

    /// <summary>
    /// Gets the transport used by the receive endpoint.
    /// </summary>
    public required MessagingTransport Transport { get; init; }
}
