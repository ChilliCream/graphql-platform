using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

internal sealed class RabbitMQTopologyProvisioner(RabbitMQMessagingTopology topology, IConnection connection)
{
    public async Task ProvisionAsync(IChannel channel, CancellationToken cancellationToken)
    {
        foreach (var exchange in topology.Exchanges)
        {
            if (topology.ShouldProvision(exchange))
            {
                await exchange.ProvisionAsync(channel, connection, cancellationToken);
            }
        }

        // An exclusive queue belongs to the connection that declares it, so only its consumer declares it and
        // the bindings that target it.
        foreach (var queue in topology.Queues)
        {
            if (!queue.Exclusive && topology.ShouldProvision(queue))
            {
                await queue.ProvisionAsync(channel, connection, cancellationToken);
            }
        }

        foreach (var binding in topology.Bindings)
        {
            if (binding is not RabbitMQQueueBinding { Destination.Exclusive: true }
                && topology.ShouldProvision(binding))
            {
                await binding.ProvisionAsync(channel, connection, cancellationToken);
            }
        }
    }

    public async Task ProvisionQueueAsync(
        RabbitMQQueue queue,
        IChannel channel,
        CancellationToken cancellationToken)
    {
        if (topology.ShouldProvision(queue))
        {
            await queue.ProvisionAsync(channel, connection, cancellationToken);
        }

        var bindings = new List<RabbitMQBinding>();
        foreach (var binding in queue.Bindings)
        {
            if (topology.ShouldProvision(binding))
            {
                bindings.Add(binding);
            }
        }

        // The list grows while it is read, which follows exchange-to-exchange bindings upstream of the queue.
        var exchanges = new HashSet<RabbitMQExchange>();
        for (var i = 0; i < bindings.Count; i++)
        {
            var exchange = bindings[i].Source;
            if (!exchanges.Add(exchange))
            {
                continue;
            }

            if (topology.ShouldProvision(exchange))
            {
                await exchange.ProvisionAsync(channel, connection, cancellationToken);
            }

            foreach (var binding in exchange.Bindings)
            {
                if (binding is RabbitMQExchangeBinding exchangeBinding
                    && exchangeBinding.Destination == exchange
                    && topology.ShouldProvision(binding))
                {
                    bindings.Add(binding);
                }
            }
        }

        foreach (var binding in bindings)
        {
            await binding.ProvisionAsync(channel, connection, cancellationToken);
        }
    }
}
