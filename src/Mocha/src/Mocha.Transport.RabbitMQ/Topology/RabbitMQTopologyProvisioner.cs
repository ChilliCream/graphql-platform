using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

internal sealed class RabbitMQTopologyProvisioner(RabbitMQMessagingTopology topology, IConnection connection)
{
    public async Task ProvisionAsync(IChannel channel, CancellationToken cancellationToken)
    {
        foreach (var exchange in topology.Exchanges)
        {
            if (exchange.AutoProvision ?? topology.AutoProvision)
            {
                await exchange.ProvisionAsync(channel, connection, cancellationToken);
            }
        }

        // An exclusive queue belongs to the connection that declares it, so only its consumer declares it and
        // the bindings that target it.
        foreach (var queue in topology.Queues)
        {
            if (!queue.Exclusive && (queue.AutoProvision ?? topology.AutoProvision))
            {
                await queue.ProvisionAsync(channel, connection, cancellationToken);
            }
        }

        foreach (var binding in topology.Bindings)
        {
            if (binding is RabbitMQQueueBinding { Destination.Exclusive: true })
            {
                continue;
            }

            await ProvisionBindingAsync(binding, channel, cancellationToken);
        }
    }

    public async Task ProvisionReceiveAsync(
        RabbitMQQueue queue,
        IChannel channel,
        CancellationToken cancellationToken)
    {
        if (queue.AutoProvision ?? topology.AutoProvision)
        {
            await queue.ProvisionAsync(channel, connection, cancellationToken);
        }

        var bindings = new List<RabbitMQBinding>();
        foreach (var binding in queue.Bindings)
        {
            if (binding.AutoProvision ?? topology.AutoProvision)
            {
                bindings.Add(binding);
            }
        }

        var exchanges = new HashSet<RabbitMQExchange>();
        for (var i = 0; i < bindings.Count; i++)
        {
            var exchange = bindings[i].Source;
            if (!exchanges.Add(exchange))
            {
                continue;
            }

            if (exchange.AutoProvision ?? topology.AutoProvision)
            {
                await exchange.ProvisionAsync(channel, connection, cancellationToken);
            }

            foreach (var binding in exchange.Bindings)
            {
                if (binding is RabbitMQExchangeBinding exchangeBinding
                    && exchangeBinding.Destination == exchange
                    && (binding.AutoProvision ?? topology.AutoProvision))
                {
                    bindings.Add(binding);
                }
            }
        }

        foreach (var binding in bindings)
        {
            await ProvisionBindingAsync(binding, channel, cancellationToken);
        }
    }

    private async Task ProvisionBindingAsync(
        RabbitMQBinding binding,
        IChannel channel,
        CancellationToken cancellationToken)
    {
        if (binding.AutoProvision ?? topology.AutoProvision)
        {
            await binding.ProvisionAsync(channel, connection, cancellationToken);
        }
    }
}
