using System.Runtime.Serialization;

namespace HotChocolate.Subscriptions;

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
