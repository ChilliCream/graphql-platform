namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Configuration for a RabbitMQ queue.
/// </summary>
public sealed class RabbitMQQueueConfiguration : TopologyConfiguration<RabbitMQMessagingTopology>
{
    /// <summary>
    /// Gets or sets the name of the queue.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the queue survives broker restarts.
    /// When true, the queue is persisted to disk and will be restored after a broker restart.
    /// Default is true.
    /// </summary>
    public bool? Durable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the queue can only be accessed by the connection that created it.
    /// When true, the queue is automatically deleted when the connection closes.
    /// Default is false.
    /// </summary>
    public bool? Exclusive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the queue is automatically deleted when no longer in use.
    /// A queue is deleted when it has no consumers and has not been used recently.
    /// Default is false.
    /// </summary>
    public bool? AutoDelete { get; set; }

    /// <summary>
    /// Gets or sets additional queue arguments for advanced configuration.
    /// Common arguments include: x-message-ttl, x-expires, x-max-length, x-max-length-bytes, x-max-priority, etc.
    /// </summary>
    public IDictionary<string, object>? Arguments { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the queue should be automatically provisioned.
    /// When true, the queue will be created in RabbitMQ during topology provisioning.
    /// Default is false.
    /// </summary>
    public bool? AutoProvision { get; set; }
}
