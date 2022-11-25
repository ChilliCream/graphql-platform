namespace HotChocolate.Subscriptions;

public enum MessageKind
{
    /// <summary>
    /// A standard message with body on the bus.
    /// </summary>
    Default,

    /// <summary>
    /// A completed message, which signals that the topic is now
    /// completed and no more messages will arrive.
    /// </summary>
    Completed,

    /// <summary>
    /// Signals that one or more subscribers are nor listening anymore.
    /// </summary>
    Unsubscribed
}
