namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Configuration for a RabbitMQ receive endpoint, specifying the source queue and consumer prefetch settings.
/// </summary>
public sealed class RabbitMQReceiveEndpointConfiguration : ReceiveEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the RabbitMQ queue name from which this endpoint consumes messages.
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of unacknowledged messages the broker will deliver to this endpoint's consumer.
    /// Defaults to 100.
    /// </summary>
    public ushort MaxPrefetch { get; set; } = 100;

    /// <summary>
    /// Gets or sets the configuration for the error queue satellite that handles failed messages.
    /// </summary>
    public RabbitMQSatelliteConfiguration ErrorQueue { get; set; } = new();

    /// <summary>
    /// Gets or sets the configuration for the skipped queue satellite that handles skipped messages.
    /// </summary>
    public RabbitMQSatelliteConfiguration SkippedQueue { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the backing queue survives broker restarts. When unset, the broker default applies.
    /// </summary>
    public bool? QueueDurable { get; set; }

    /// <summary>
    /// Gets or sets whether the backing queue is automatically provisioned. When unset, the transport setting applies.
    /// </summary>
    public bool? QueueAutoProvision { get; set; }

    /// <summary>
    /// Gets or sets additional arguments applied to the backing queue, such as <c>x-queue-type</c> for a quorum queue.
    /// </summary>
    public IDictionary<string, object>? QueueArguments { get; set; }
}
