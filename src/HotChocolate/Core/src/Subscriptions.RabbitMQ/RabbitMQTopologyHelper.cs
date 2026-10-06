using System.Collections.Concurrent;
using RabbitMQ.Client;

namespace HotChocolate.Subscriptions.RabbitMQ;

internal class RabbitMQTopologyHelper
{
    private readonly ConcurrentDictionary<string, bool> _declaredExchanges = new();

    public async ValueTask ConfigurePublishingAsync(IChannel channel, string formattedTopic, CancellationToken cancellationToken)
    {
        // Create an exchange if it wasn't created.
        // This extra check isn't required, but it's faster to do it in memory than go to RabbitMQ every time before publishing a message
        if (!_declaredExchanges.ContainsKey(formattedTopic))
        {
            await channel.ExchangeDeclareAsync(
                exchange: formattedTopic,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            _declaredExchanges.TryAdd(formattedTopic, true);
        }
    }

    public async Task ConfigureConsumingAsync(IChannel channel, string formattedTopic, string queueName, CancellationToken cancellationToken)
    {
        // need to declare an exchange so that we can bind a queue to it
        await ConfigurePublishingAsync(channel, formattedTopic, cancellationToken);

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: true,
            autoDelete: true,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: queueName,
            exchange: formattedTopic,
            routingKey: string.Empty,
            cancellationToken: cancellationToken);
    }
}
