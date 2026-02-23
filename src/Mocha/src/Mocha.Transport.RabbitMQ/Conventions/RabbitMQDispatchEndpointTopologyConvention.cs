namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Convention that auto-provisions exchanges and queues in the topology for dispatch endpoints
/// when they do not already exist.
/// </summary>
public sealed class RabbitMQDispatchEndpointTopologyConvention : IRabbitMQDispatchEndpointTopologyConvention
{
    /// <summary>
    /// Discovers and creates missing topology resources (exchanges or queues) needed by the dispatch endpoint.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="endpoint">The dispatch endpoint being configured.</param>
    /// <param name="configuration">The endpoint configuration specifying the target exchange or queue name.</param>
    public void DiscoverTopology(
        IMessagingConfigurationContext context,
        RabbitMQDispatchEndpoint endpoint,
        RabbitMQDispatchEndpointConfiguration configuration)
    {
        var topology = (RabbitMQMessagingTopology)endpoint.Transport.Topology;

        if (configuration.ExchangeName is not null
            && topology.Exchanges.FirstOrDefault(e => e.Name == configuration.ExchangeName) is null)
        {
            topology.AddExchange(new RabbitMQExchangeConfiguration { Name = configuration.ExchangeName });
        }

        if (configuration.QueueName is not null
            && topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName) is null)
        {
            topology.AddQueue(new RabbitMQQueueConfiguration { Name = configuration.QueueName });
        }
    }
}
