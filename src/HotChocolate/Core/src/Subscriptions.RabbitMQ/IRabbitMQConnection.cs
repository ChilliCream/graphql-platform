using RabbitMQ.Client;

namespace HotChocolate.Subscriptions.RabbitMQ;

internal interface IRabbitMQConnection
{
    Task<IChannel> GetChannelAsync(CancellationToken cancellationToken);
}
