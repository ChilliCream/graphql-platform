namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Specifies the behavior when a queue reaches its maximum length.
/// </summary>
public enum RabbitMQQueueOverFlowBehavior
{
    /// <summary>
    /// Drop the oldest messages from the head of the queue (default).
    /// </summary>
    DropHead,

    /// <summary>
    /// Reject new messages when the queue is full.
    /// </summary>
    RejectPublish
}
