using System.Runtime.Serialization;

namespace HotChocolate.Subscriptions;

/// <summary>
/// This exception is thrown if a subscribe attempt is being made to an existing topic
/// with a different message type.
/// </summary>
[Serializable]
public class InvalidMessageTypeException : Exception
{
    public InvalidMessageTypeException() { }

    public InvalidMessageTypeException(string message)
        : base(message) { }

    public InvalidMessageTypeException(string message, Exception inner)
        : base(message, inner) { }

    protected InvalidMessageTypeException(
        SerializationInfo info,
        StreamingContext context)
        : base(info, context) { }
}
