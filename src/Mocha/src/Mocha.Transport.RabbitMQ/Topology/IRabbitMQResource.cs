using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Represents a RabbitMQ topology resource (exchange, queue, or binding) that can be provisioned on the broker.
/// </summary>
public interface IRabbitMQResource
{
    /// <summary>
    /// Declares this resource on the broker using the specified channel.
    /// </summary>
    /// <param name="channel">The RabbitMQ channel to use for declaring the resource.</param>
    /// <param name="cancellationToken">A token to cancel the provisioning operation.</param>
    Task ProvisionAsync(IChannel channel, CancellationToken cancellationToken);
}
