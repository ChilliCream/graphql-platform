namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Configuration for a RabbitMQ dispatch endpoint, specifying the target queue or exchange for outbound messages.
/// </summary>
/// <remarks>
/// Exactly one of <see cref="QueueName"/> or <see cref="ExchangeName"/> should be set.
/// When <see cref="QueueName"/> is set, messages are published directly to the default exchange with the queue name as routing key.
/// When <see cref="ExchangeName"/> is set, messages are published to the named exchange.
/// </remarks>
public sealed class RabbitMQDispatchEndpointConfiguration : DispatchEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the target queue name for direct-to-queue dispatch. Mutually exclusive with <see cref="ExchangeName"/>.
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Gets or sets the target exchange name for exchange-based dispatch. Mutually exclusive with <see cref="QueueName"/>.
    /// </summary>
    public string? ExchangeName { get; set; }
}
