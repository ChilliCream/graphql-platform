namespace Mocha;

/// <summary>
/// Specifies the kind of outbound message route.
/// </summary>
public enum OutboundRouteKind
{
    /// <summary>
    /// A point-to-point send operation targeting a specific endpoint.
    /// </summary>
    Send,

    /// <summary>
    /// A publish operation distributing the message to all subscribers.
    /// </summary>
    Publish
}
