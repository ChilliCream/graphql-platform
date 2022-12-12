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

    public RabbitMQConnectionFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
