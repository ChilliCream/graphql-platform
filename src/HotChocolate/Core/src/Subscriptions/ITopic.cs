namespace HotChocolate.Subscriptions;

/// <summary>
/// Represents a topic.
/// </summary>
public interface ITopic : IDisposable
{
    /// <summary>
    /// Gets the message type of this topic.
    /// </summary>
    public Type MessageType { get; }

    /// <summary>
    /// Allows to complete a topic.
    /// </summary>
    void Complete();
}
