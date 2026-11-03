using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

internal sealed class RabbitMQTopologyProvisioner(RabbitMQMessagingTopology topology)
{
    public async Task ProvisionAsync(IChannel channel, CancellationToken cancellationToken)
    {
        foreach (var exchange in topology.Exchanges)
        {
            if (exchange.AutoProvision ?? topology.AutoProvision)
            {
                await exchange.ProvisionAsync(channel, cancellationToken);
            }
        }

        foreach (var queue in topology.Queues)
        {
            if (queue.AutoProvision ?? topology.AutoProvision)
            {
                await queue.ProvisionAsync(channel, cancellationToken);
            }
        }

        foreach (var binding in topology.Bindings)
        {
            await ProvisionBindingAsync(binding, channel, cancellationToken);
        }
    }

    public async Task ProvisionReceiveAsync(
        RabbitMQQueue queue,
        IChannel channel,
        CancellationToken cancellationToken)
    {
        if (!(queue.AutoProvision ?? topology.AutoProvision))
        {
            return;
        }

        await queue.ProvisionAsync(channel, cancellationToken);

        foreach (var binding in queue.Bindings)
        {
            if (binding.AutoProvision ?? topology.AutoProvision)
            {
                if (binding.Source.AutoProvision ?? topology.AutoProvision)
                {
                    await binding.Source.ProvisionAsync(channel, cancellationToken);
                }

                await ProvisionBindingAsync(binding, channel, cancellationToken);
            }
        }
    }

    private async Task ProvisionBindingAsync(
        RabbitMQBinding binding,
        IChannel channel,
        CancellationToken cancellationToken)
    {
        if (binding.AutoProvision ?? topology.AutoProvision)
        {
            await binding.ProvisionAsync(channel, cancellationToken);
        }
    }
}
