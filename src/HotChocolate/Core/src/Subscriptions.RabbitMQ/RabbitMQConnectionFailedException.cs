using static HotChocolate.Subscriptions.RabbitMQ.RabbitMQResources;

namespace HotChocolate.Subscriptions.RabbitMQ;

internal sealed class RabbitMQConnectionFailedException : Exception
{
    public RabbitMQConnectionFailedException(int connectionAttempts)
        : base(string.Format(
            RabbitMQConnectionFailedException_RabbitMQConnectionFailedException_ConnectionFailed,
            connectionAttempts))
    {
    }
}
