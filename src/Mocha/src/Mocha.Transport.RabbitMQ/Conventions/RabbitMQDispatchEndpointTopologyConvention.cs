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

        // Bind custom dispatch exchanges to convention exchanges so routing is consistent
        // across sender/receiver boundaries.
        if (configuration.ExchangeName is not null)
        {
            foreach (var (runtimeType, kind) in configuration.Routes)
            {
                var conventionExchangeName =
                    kind == OutboundRouteKind.Publish
                        ? context.Naming.GetPublishEndpointName(runtimeType)
                        : context.Naming.GetSendEndpointName(runtimeType);

                if (configuration.ExchangeName == conventionExchangeName)
                {
                    continue;
                }

                if (topology.Exchanges.FirstOrDefault(e => e.Name == conventionExchangeName) is null)
                {
                    topology.AddExchange(new RabbitMQExchangeConfiguration { Name = conventionExchangeName });
                }

                if (topology.Bindings.FirstOrDefault(b =>
                        b.Source.Name == configuration.ExchangeName
                        && b is RabbitMQExchangeBinding exchangeBinding
                        && exchangeBinding.Destination.Name == conventionExchangeName
                    )
                    is null)
                {
                    topology.AddBinding(
                        new RabbitMQBindingConfiguration
                        {
                            Source = configuration.ExchangeName,
                            Destination = conventionExchangeName,
                            DestinationKind = RabbitMQDestinationKind.Exchange
                        });
                }
            }
        }
    }
}
