using Mocha.Features;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ.Features;

/// <summary>
/// Pooled feature that carries the RabbitMQ channel and delivery event args through the receive middleware pipeline,
/// enabling acknowledgement and message parsing middleware to access the raw delivery context.
/// </summary>
public sealed class RabbitMQReceiveFeature : IPooledFeature
{
    /// <summary>
    /// Gets or sets the RabbitMQ channel on which the message was delivered.
    /// </summary>
    public IChannel Channel { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delivery event args containing the message body, properties, and delivery tag.
    /// </summary>
    public BasicDeliverEventArgs EventArgs { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ids of messages whose handler completed but whose delivery could not be acknowledged, or
    /// <c>null</c> when the endpoint does not track them.
    /// </summary>
    internal RabbitMQCompletedDeliveries? CompletedDeliveries { get; set; }

    /// <inheritdoc />
    public void Initialize(object state)
    {
        Channel = null!;
        EventArgs = null!;
        CompletedDeliveries = null;
    }

    /// <inheritdoc />
    public void Reset()
    {
        Channel = null!;
        EventArgs = null!;
        CompletedDeliveries = null;
    }
}
