using RabbitMQ.Client;

namespace HotChocolate.Subscriptions.RabbitMQ;

internal interface IRabbitMQConnection
{
    Task<IModel> GetChannelAsync();
}
