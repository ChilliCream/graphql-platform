using System.Runtime.Serialization;

namespace HotChocolate.Subscriptions;

/// <summary>
/// This exception is being thrown if the GraphQL execution engine cannot subscribe to a topic.
/// </summary>
[Serializable]
public class CannotSubscribeException : Exception
{
    public CannotSubscribeException() { }

    public CannotSubscribeException(string message)
        : base(message) { }

    public CannotSubscribeException(string message, Exception inner)
        : base(message, inner) { }

    protected CannotSubscribeException(
        SerializationInfo info,
        StreamingContext context)
        : base(info, context) { }
}
