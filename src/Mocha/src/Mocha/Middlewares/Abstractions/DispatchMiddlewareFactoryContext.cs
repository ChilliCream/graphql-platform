namespace Mocha;

/// <summary>
/// Provides context for constructing a dispatch middleware pipeline, including the service provider, endpoint, and transport.
/// </summary>
public class DispatchMiddlewareFactoryContext
{
    /// <summary>
    /// Gets the service provider for resolving dependencies.
    /// </summary>
    public required IServiceProvider Services { get; init; }

    /// <summary>
    /// Gets the dispatch endpoint that this middleware pipeline is being built for.
    /// </summary>
    public required DispatchEndpoint Endpoint { get; init; }

    /// <summary>
    /// Gets the transport used by the dispatch endpoint.
    /// </summary>
    public required MessagingTransport Transport { get; init; }
}
