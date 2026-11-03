using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

internal sealed class RabbitMQReceiveTopology(RabbitMQMessagingTopology topology, RabbitMQQueue queue)
{
    private readonly RabbitMQTopologyProvisioner _provisioner = new(topology);

    public bool AutoProvision => queue.AutoProvision ?? topology.AutoProvision;

    public Task ProvisionAsync(IChannel channel, CancellationToken cancellationToken)
        => _provisioner.ProvisionReceiveAsync(queue, channel, cancellationToken);
}
