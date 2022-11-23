using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using StackExchange.Redis;

namespace HotChocolate.Subscriptions.Redis;

internal sealed class RedisPubSub : ITopicEventReceiver, ITopicEventSender
{
    private readonly IConnectionMultiplexer _connection;
    private readonly IMessageSerializer _messageSerializer;

    public RedisPubSub(IConnectionMultiplexer connection, IMessageSerializer messageSerializer)
    {
        _connection = connection ??
            throw new ArgumentNullException(nameof(connection));
        _messageSerializer = messageSerializer ??
            throw new ArgumentNullException(nameof(messageSerializer));
    }

    public async ValueTask<ISourceStream<TMessage>> SubscribeAsync<TMessage>(
        string topic,
        CancellationToken cancellationToken = default)
    {
        var subscriber = _connection.GetSubscriber();
        var channel = await subscriber.SubscribeAsync(topic).ConfigureAwait(false);
        return new RedisSourceStream<TMessage>(channel, _messageSerializer);
    }

    public async ValueTask SendAsync<TMessage>(
        string topic,
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        var subscriber = _connection.GetSubscriber();
        var serializedMessage = _messageSerializer.Serialize(new EventMessage<TMessage>(message));
        await subscriber.PublishAsync(topic, serializedMessage).ConfigureAwait(false);
    }

    public async ValueTask CompleteAsync(string topic)
    {
        var subscriber = _connection.GetSubscriber();
        var message = _messageSerializer.CompleteMessage;
        await subscriber.PublishAsync(topic, message).ConfigureAwait(false);
    }
}
