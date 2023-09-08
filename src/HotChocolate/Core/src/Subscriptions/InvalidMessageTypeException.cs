using HotChocolate.Subscriptions.Properties;

namespace HotChocolate.Subscriptions;

/// <summary>
/// This exception is thrown if a subscribe attempt is being made to an existing topic
/// with a different message type.
/// </summary>
[Serializable]
public class InvalidMessageTypeException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidMessageTypeException"/> class.
    /// </summary>
    /// <param name="topicMessageType">
    /// The topic message type.
    /// </param>
    /// <param name="requestedMessageType">
    /// The requested message type.
    /// </param>
    public InvalidMessageTypeException(Type topicMessageType, Type requestedMessageType)
        : base(CreateMessage(topicMessageType, requestedMessageType))
    {
        TopicMessageType = topicMessageType;
        RequestedMessageType = requestedMessageType;
    }

    /// <summary>
    /// Gets the topic message type.
    /// </summary>
    public Type TopicMessageType { get; }

    /// <summary>
    /// Gets the requested message type.
    /// </summary>
    public Type RequestedMessageType { get; }

    private static string CreateMessage(
        Type topicMessageType,
        Type requestedMessageType)
        => string.Format(
            Resources.InvalidMessageTypeException_Message,
            topicMessageType.FullName,
            requestedMessageType.FullName);
}
