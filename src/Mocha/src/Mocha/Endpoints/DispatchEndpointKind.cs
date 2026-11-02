namespace Mocha;

/// <summary>
/// Specifies the kind of dispatch endpoint, determining how outbound messages are routed.
/// </summary>
public enum DispatchEndpointKind
{
    /// <summary>
    /// A standard dispatch endpoint for normal message sending.
    /// </summary>
    Default,

    /// <summary>
    /// A reply dispatch endpoint used to send responses back to the requester.
    /// </summary>
    Reply
}
