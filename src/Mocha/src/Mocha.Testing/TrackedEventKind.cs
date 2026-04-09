namespace Mocha.Testing;

/// <summary>
/// Describes the kind of event in the message tracking timeline.
/// </summary>
public enum TrackedEventKind
{
    /// <summary>
    /// A message was dispatched (published or sent).
    /// </summary>
    Dispatched,

    /// <summary>
    /// A message was received by a consumer endpoint.
    /// </summary>
    Received,

    /// <summary>
    /// A consumer started processing a message.
    /// </summary>
    ConsumeStarted,

    /// <summary>
    /// A consumer completed processing a message successfully.
    /// </summary>
    ConsumeCompleted,

    /// <summary>
    /// A consumer failed to process a message.
    /// </summary>
    ConsumeFailed
}
