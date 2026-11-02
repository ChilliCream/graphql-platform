namespace Mocha;

/// <summary>
/// Specifies the kind of receive endpoint, determining how messages are accepted and processed.
/// </summary>
public enum ReceiveEndpointKind
{
    /// <summary>
    /// A standard receive endpoint that processes messages normally.
    /// </summary>
    Default,

    /// <summary>
    /// An error endpoint that receives messages that failed processing on another endpoint.
    /// </summary>
    Error,

    /// <summary>
    /// An endpoint that receives messages that were skipped (unroutable) on another endpoint.
    /// </summary>
    Skipped,

    /// <summary>
    /// A reply endpoint that receives response messages for request-reply patterns.
    /// </summary>
    Reply
}
