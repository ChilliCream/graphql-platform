using RabbitMQ.Client;

namespace HotChocolate.Subscriptions.RabbitMQ;

internal interface IRabbitMQConnection
{
    public Task<IModel> GetChannelAsync();
}
