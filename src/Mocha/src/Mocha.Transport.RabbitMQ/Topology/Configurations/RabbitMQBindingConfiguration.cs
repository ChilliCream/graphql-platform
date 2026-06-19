namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Configuration for a RabbitMQ binding that connects an exchange to a queue or another exchange.
/// </summary>
public sealed class RabbitMQBindingConfiguration : TopologyConfiguration
{
    /// <summary>
    /// Gets or sets the name of the source exchange.
    /// This is the exchange from which messages will be routed.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the destination queue or exchange.
    /// This is where messages will be routed to based on the binding rules.
    /// </summary>
    public string Destination { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the kind of destination (queue or exchange) for this binding.
    /// </summary>
    public RabbitMQDestinationKind DestinationKind { get; set; }

    /// <summary>
    /// Gets or sets the routing key used for message routing.
    /// The routing key is matched against binding keys to determine message delivery.
    /// For direct exchanges, this must match exactly. For topic exchanges, wildcards are supported.
    /// </summary>
    public string? RoutingKey { get; set; }

    /// <summary>
    /// Gets or sets additional binding arguments for advanced routing configuration.
    /// Used for headers exchange routing and other advanced routing scenarios.
    /// </summary>
    public IDictionary<string, object>? Arguments { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the binding should be automatically provisioned.
    /// When true, the binding will be created in RabbitMQ during topology provisioning.
    /// Default is false.
    /// </summary>
    public bool? AutoProvision { get; set; }
}

/// <summary>
/// Specifies whether a binding destination is a queue or an exchange.
/// </summary>
public enum RabbitMQDestinationKind
{
    /// <summary>
    /// The binding destination is a queue.
    /// </summary>
    Queue,

    /// <summary>
    /// The binding destination is an exchange.
    /// </summary>
    Exchange
}
