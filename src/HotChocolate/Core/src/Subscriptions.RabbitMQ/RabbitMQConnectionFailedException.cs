using System.Runtime.Serialization;
using static HotChocolate.Subscriptions.RabbitMQ.RabbitMQResources;

namespace HotChocolate.Subscriptions.RabbitMQ;

[Serializable]
internal sealed class RabbitMQConnectionFailedException : Exception
{
    public RabbitMQConnectionFailedException(int connectionAttempts)
        : base(string.Format(
            RabbitMQConnectionFailedException_RabbitMQConnectionFailedException_ConnectionFailed,
            connectionAttempts))
    {
    }

#if NET8_0_OR_GREATER
    [Obsolete(
        "This API supports obsolete formatter-based serialization. " +
        "It should not be called or extended by application code.",
        true)]
#endif
    public RabbitMQConnectionFailedException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
