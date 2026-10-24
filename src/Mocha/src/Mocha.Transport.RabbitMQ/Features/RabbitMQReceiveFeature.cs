using Mocha.Features;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ.Features;

public sealed class RabbitMQReceiveFeature : IPooledFeature
{
    public IChannel Channel { get; set; } = null!;

    public BasicDeliverEventArgs EventArgs { get; set; } = null!;

    internal RabbitMQCompletedDeliveries? CompletedDeliveries { get; set; }

    public void Initialize(object state)
    {
        Channel = null!;
        EventArgs = null!;
        CompletedDeliveries = null;
    }

    public void Reset()
    {
        Channel = null!;
        EventArgs = null!;
        CompletedDeliveries = null;
    }
}
