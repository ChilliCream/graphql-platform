namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Convention that auto-provisions queues, exchanges, and bindings in the topology for receive endpoints,
/// creating the necessary publish and send exchange hierarchy and queue bindings for each inbound route.
/// </summary>
public sealed class RabbitMQReceiveEndpointTopologyConvention : IRabbitMQReceiveEndpointTopologyConvention
{
    /// <summary>
    /// Discovers and creates missing topology resources (queues, exchanges, bindings) needed by the receive endpoint
    /// based on its inbound message routes.
    /// </summary>
    /// <param name="context">The messaging configuration context providing naming and routing information.</param>
    /// <param name="endpoint">The receive endpoint being configured.</param>
    /// <param name="configuration">The endpoint configuration specifying the source queue name.</param>
    /// <exception cref="InvalidOperationException">Thrown if the queue name is not set on the configuration.</exception>
    public void DiscoverTopology(
        IMessagingConfigurationContext context,
        RabbitMQReceiveEndpoint endpoint,
        RabbitMQReceiveEndpointConfiguration configuration)
    {
        if (configuration.QueueName is null)
        {
            throw new InvalidOperationException("Queue name is required");
        }

        var topology = (RabbitMQMessagingTopology)endpoint.Transport.Topology;

        if (topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName) is null)
        {
            topology.AddQueue(
                new RabbitMQQueueConfiguration
                {
                    Name = configuration.QueueName,
                    AutoDelete = endpoint.Kind == ReceiveEndpointKind.Reply,
                    AutoProvision = configuration.AutoProvision
                });
        }

        if (endpoint.Kind is ReceiveEndpointKind.Reply or ReceiveEndpointKind.Error or ReceiveEndpointKind.Skipped)
        {
            return;
        }

        var routes = context.Router.GetInboundByEndpoint(endpoint);
        foreach (var route in routes)
        {
            if (route.MessageType is null)
            {
                continue;
            }

            var publishExchangeName = context.Naming.GetPublishEndpointName(route.MessageType.RuntimeType);
            if (topology.Exchanges.FirstOrDefault(e => e.Name == publishExchangeName) is null)
            {
                topology.AddExchange(new RabbitMQExchangeConfiguration { Name = publishExchangeName });
            }

            // make sure the exchange for the message type exists
            var sendExchangeName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
            if (sendExchangeName != publishExchangeName)
            {
                if (topology.Exchanges.FirstOrDefault(e => e.Name == sendExchangeName) is null)
                {
                    topology.AddExchange(new RabbitMQExchangeConfiguration { Name = sendExchangeName });
                }

                // make sure the binding between the publish exchange and the send exchange exists
                if (topology.Bindings.FirstOrDefault(b =>
                        b.Source.Name == publishExchangeName
                        && b is RabbitMQExchangeBinding exchangeBinding
                        && exchangeBinding.Destination.Name == sendExchangeName
                    )
                    is null)
                {
                    topology.AddBinding(
                        new RabbitMQBindingConfiguration
                        {
                            Source = publishExchangeName,
                            Destination = sendExchangeName,
                            DestinationKind = RabbitMQDestinationKind.Exchange
                        });
                }
            }

            // make sure the binding between the exchange and the queue exists
            if (topology.Bindings.FirstOrDefault(b =>
                    b.Source.Name == sendExchangeName
                    && b is RabbitMQExchangeBinding exchangeBinding
                    && exchangeBinding.Destination.Name == configuration.QueueName
                )
                is null)
            {
                topology.AddBinding(
                    new RabbitMQBindingConfiguration
                    {
                        Source = sendExchangeName,
                        Destination = configuration.QueueName,
                        DestinationKind = RabbitMQDestinationKind.Queue
                    });
            }
        }
    }
}
