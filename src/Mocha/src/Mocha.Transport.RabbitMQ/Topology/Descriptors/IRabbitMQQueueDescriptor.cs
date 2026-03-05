namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Fluent interface for configuring a RabbitMQ queue.
/// </summary>
public interface IRabbitMQQueueDescriptor : IMessagingDescriptor<RabbitMQQueueConfiguration>
{
    /// <summary>
    /// Sets the name of the queue.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor Name(string name);

    /// <summary>
    /// Sets whether the queue survives broker restarts.
    /// </summary>
    /// <param name="durable">True to make the queue durable (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor Durable(bool durable = true);

    /// <summary>
    /// Sets whether the queue is exclusive to the connection that created it.
    /// </summary>
    /// <param name="exclusive">True to make the queue exclusive (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor Exclusive(bool exclusive = true);

    /// <summary>
    /// Sets whether the queue is automatically deleted when no longer in use.
    /// </summary>
    /// <param name="autoDelete">True to enable auto-deletion (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor AutoDelete(bool autoDelete = true);

    /// <summary>
    /// Adds a custom argument to the queue configuration.
    /// </summary>
    /// <param name="key">The argument key (e.g., "x-message-ttl", "x-max-length").</param>
    /// <param name="value">The argument value.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor WithArgument(string key, object value);

    /// <summary>
    /// Sets whether the queue should be automatically provisioned.
    /// </summary>
    /// <param name="autoProvision">True to enable auto-provisioning (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor AutoProvision(bool autoProvision = true);
}
