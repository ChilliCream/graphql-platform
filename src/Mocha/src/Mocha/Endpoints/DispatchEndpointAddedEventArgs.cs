namespace Mocha;

/// <summary>
/// Provides data for the <see cref="IEndpointRouter.DispatchEndpointAdded"/> event,
/// which is raised after a new <see cref="DispatchEndpoint"/> is added to the router.
/// </summary>
public sealed class DispatchEndpointAddedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="DispatchEndpointAddedEventArgs"/>.
    /// </summary>
    /// <param name="endpoint">The dispatch endpoint that was added.</param>
    public DispatchEndpointAddedEventArgs(DispatchEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        Endpoint = endpoint;
    }

    /// <summary>
    /// Gets the dispatch endpoint that was added to the router.
    /// </summary>
    public DispatchEndpoint Endpoint { get; }
}
