namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Configuration for a RabbitMQ exchange.
/// </summary>
public sealed class RabbitMQExchangeConfiguration : TopologyConfiguration<RabbitMQMessagingTopology>
{
    /// <summary>
    /// Gets or sets the name of the exchange.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the exchange.
    /// Determines how messages are routed to queues (Direct, Fanout, Topic, or Headers).
    /// Default is Direct.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the exchange survives broker restarts.
    /// When true, the exchange is persisted to disk and will be restored after a broker restart.
    /// Default is true.
    /// </summary>
    public bool? Durable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the exchange is automatically deleted when no longer in use.
    /// An exchange is deleted when it has no queue bindings and has not been used recently.
    /// Default is false.
    /// </summary>
    public bool? AutoDelete { get; set; }

    /// <summary>
    /// Gets or sets additional exchange arguments for advanced configuration.
    /// Common arguments include: alternate-exchange, etc.
    /// </summary>
    public IDictionary<string, object>? Arguments { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the exchange should be automatically provisioned.
    /// When true, the exchange will be created in RabbitMQ during topology provisioning.
    /// Default is false.
    /// </summary>
    public bool? AutoProvision { get; set; }
}
