namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Extension methods for RabbitMQ descriptor configuration.
/// </summary>
public static class RabbitMQQueueTopologyDescriptorExtensions
{
    /// <summary>
    /// Sets the message time-to-live (TTL) for messages in the queue.
    /// Messages that remain in the queue longer than this duration will be discarded.
    /// </summary>
    /// <param name="descriptor">The queue descriptor.</param>
    /// <param name="ttl">The time-to-live duration.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IRabbitMQQueueTopologyDescriptor MessageTimeToLive(this IRabbitMQQueueTopologyDescriptor descriptor, TimeSpan ttl)
    {
        return descriptor.WithArgument("x-message-ttl", (int)ttl.TotalMilliseconds);
    }

    /// <summary>
    /// Sets the queue expiration time.
    /// The queue will be automatically deleted after this duration if it has no consumers.
    /// </summary>
    /// <param name="descriptor">The queue descriptor.</param>
    /// <param name="expiry">The expiration duration.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IRabbitMQQueueTopologyDescriptor Expires(this IRabbitMQQueueTopologyDescriptor descriptor, TimeSpan expiry)
    {
        return descriptor.WithArgument("x-expires", (int)expiry.TotalMilliseconds);
    }

    /// <summary>
    /// Sets the maximum number of messages in the queue.
    /// When the limit is reached, messages are handled according to the overflow behavior.
    /// </summary>
    /// <param name="descriptor">The queue descriptor.</param>
    /// <param name="messageCount">The maximum number of messages.</param>
    /// <param name="overflowBehaviour">The behavior when the limit is reached (default: DropHead).</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IRabbitMQQueueTopologyDescriptor MaxLength(
        this IRabbitMQQueueTopologyDescriptor descriptor,
        int messageCount,
        RabbitMQQueueOverFlowBehavior overflowBehaviour = RabbitMQQueueOverFlowBehavior.DropHead)
    {
        return descriptor
            .WithArgument("x-max-length", messageCount)
            .WithArgument(
                "x-overflow",
                overflowBehaviour == RabbitMQQueueOverFlowBehavior.DropHead ? "drop-head" : "reject-publish");
    }

    /// <summary>
    /// Sets the maximum total size of messages in the queue (in bytes).
    /// When the limit is reached, messages are handled according to the overflow behavior.
    /// </summary>
    /// <param name="descriptor">The queue descriptor.</param>
    /// <param name="messageBytes">The maximum size in bytes.</param>
    /// <param name="overflowBehaviour">The behavior when the limit is reached (default: DropHead).</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IRabbitMQQueueTopologyDescriptor MaxLengthBytes(
        this IRabbitMQQueueTopologyDescriptor descriptor,
        long messageBytes,
        RabbitMQQueueOverFlowBehavior overflowBehaviour = RabbitMQQueueOverFlowBehavior.DropHead)
    {
        return descriptor
            .WithArgument("x-max-length-bytes", messageBytes)
            .WithArgument(
                "x-overflow",
                overflowBehaviour == RabbitMQQueueOverFlowBehavior.DropHead ? "drop-head" : "reject-publish");
    }

    /// <summary>
    /// Configures a dead letter exchange and routing key for messages that are rejected or expire.
    /// </summary>
    /// <param name="descriptor">The queue descriptor.</param>
    /// <param name="exchange">The dead letter exchange name.</param>
    /// <param name="routingKey">The routing key for dead lettered messages.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IRabbitMQQueueTopologyDescriptor DeadLetter(
        this IRabbitMQQueueTopologyDescriptor descriptor,
        string exchange,
        string routingKey)
    {
        return descriptor
            .WithArgument("x-dead-letter-exchange", exchange)
            .WithArgument("x-dead-letter-routing-key", routingKey);
    }

    /// <summary>
    /// Sets the queue type (Classic, Quorum, or Stream).
    /// </summary>
    /// <param name="descriptor">The queue descriptor.</param>
    /// <param name="queueType">The queue type.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IRabbitMQQueueTopologyDescriptor QueueType(this IRabbitMQQueueTopologyDescriptor descriptor, string queueType)
    {
        return descriptor.WithArgument("x-queue-type", queueType);
    }

    /// <summary>
    /// Sets the queue mode (Default or Lazy).
    /// Lazy mode keeps messages on disk and loads them into memory when needed.
    /// </summary>
    /// <param name="descriptor">The queue descriptor.</param>
    /// <param name="mode">The queue mode.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IRabbitMQQueueTopologyDescriptor QueueMode(this IRabbitMQQueueTopologyDescriptor descriptor, RabbitMQQueueMode mode)
    {
        return descriptor.WithArgument("x-queue-mode", mode == RabbitMQQueueMode.Lazy ? "lazy" : "default");
    }

    /// <summary>
    /// Enables single active consumer mode.
    /// Only one consumer at a time will receive messages from the queue.
    /// </summary>
    /// <param name="descriptor">The queue descriptor.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IRabbitMQQueueTopologyDescriptor SingleActiveConsumer(this IRabbitMQQueueTopologyDescriptor descriptor)
    {
        return descriptor.WithArgument("x-single-active-consumer", true);
    }

    /// <summary>
    /// Sets the maximum priority level for messages in the queue.
    /// Messages with higher priority will be delivered before messages with lower priority.
    /// </summary>
    /// <param name="descriptor">The queue descriptor.</param>
    /// <param name="level">The maximum priority level (default: 255).</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IRabbitMQQueueTopologyDescriptor MaxPriority(this IRabbitMQQueueTopologyDescriptor descriptor, int level = 255)
    {
        return descriptor.WithArgument("x-max-priority", level);
    }

    /// <summary>
    /// Sets the initial group size for a quorum queue.
    /// This determines how many replicas the queue will have.
    /// </summary>
    /// <param name="descriptor">The queue descriptor.</param>
    /// <param name="size">The initial group size (number of replicas).</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IRabbitMQQueueTopologyDescriptor QuorumInitialGroupSize(this IRabbitMQQueueTopologyDescriptor descriptor, int size)
    {
        return descriptor.WithArgument("x-quorum-initial-group-size", size);
    }

    /// <summary>
    /// Sets the maximum delivery limit for messages in the queue.
    /// Messages that exceed this limit will be dead-lettered or discarded.
    /// </summary>
    /// <param name="descriptor">The queue descriptor.</param>
    /// <param name="limit">The maximum number of delivery attempts.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IRabbitMQQueueTopologyDescriptor MaxDeliveryLimit(this IRabbitMQQueueTopologyDescriptor descriptor, int limit)
    {
        return descriptor.WithArgument("x-delivery-limit", limit);
    }
}
