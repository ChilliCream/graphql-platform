namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Configuration for a source exchange binding declared from a queue descriptor.
/// </summary>
public sealed class RabbitMQQueueSourceBindingConfiguration
{
    /// <summary>
    /// Gets or sets the source address.
    /// </summary>
    public Uri Source { get; set; } = null!;

    /// <summary>
    /// Gets or sets the optional routing key.
    /// </summary>
    public string? RoutingKey { get; set; }

    /// <summary>
    /// Gets or sets whether the derived binding is provisioned. <c>null</c> inherits the queue's
    /// setting, falling back to the transport default.
    /// </summary>
    public bool? AutoProvision { get; set; }
}
