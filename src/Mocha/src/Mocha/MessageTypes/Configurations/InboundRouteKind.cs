namespace Mocha;

/// <summary>
/// Specifies the kind of inbound message route.
/// </summary>
public enum InboundRouteKind
{
    /// <summary>
    /// A publish-subscribe route where the consumer subscribes to a message type.
    /// </summary>
    Subscribe,

    /// <summary>
    /// A point-to-point send route where the consumer receives direct messages.
    /// </summary>
    Send,

    /// <summary>
    /// A request route where the consumer handles requests and produces responses.
    /// </summary>
    Request,

    /// <summary>
    /// A reply route where the consumer receives responses to previous requests.
    /// </summary>
    Reply
}
