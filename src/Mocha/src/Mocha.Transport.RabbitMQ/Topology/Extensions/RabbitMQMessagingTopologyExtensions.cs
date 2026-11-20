namespace Mocha.Transport.RabbitMQ;

internal static class RabbitMQMessagingTopologyExtensions
{
    /// <summary>
    /// Gets whether the exchange is auto-provisioned, by its own setting or else the topology default.
    /// </summary>
    public static bool ShouldProvision(this RabbitMQMessagingTopology topology, RabbitMQExchange exchange)
        => exchange.AutoProvision ?? topology.AutoProvision;

    /// <summary>
    /// Gets whether the queue is auto-provisioned, by its own setting or else the topology default.
    /// </summary>
    public static bool ShouldProvision(this RabbitMQMessagingTopology topology, RabbitMQQueue queue)
        => queue.AutoProvision ?? topology.AutoProvision;

    /// <summary>
    /// Gets whether the binding is auto-provisioned, by its own setting or else the topology default.
    /// </summary>
    public static bool ShouldProvision(this RabbitMQMessagingTopology topology, RabbitMQBinding binding)
        => binding.AutoProvision ?? topology.AutoProvision;
}
