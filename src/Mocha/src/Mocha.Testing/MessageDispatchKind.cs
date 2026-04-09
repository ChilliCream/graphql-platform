namespace Mocha.Testing;

/// <summary>
/// Describes how a message was dispatched.
/// </summary>
public enum MessageDispatchKind
{
    /// <summary>
    /// The message was published to all subscribers.
    /// </summary>
    Published,

    /// <summary>
    /// The message was sent to a specific endpoint.
    /// </summary>
    Sent
}
