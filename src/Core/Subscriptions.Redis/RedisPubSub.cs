using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace HotChocolate.Subscriptions.Redis
{
    public class RedisPubSub
        : ITopicEventReceiver
        , ITopicEventSender
    {
        private readonly IConnectionMultiplexer _connection;
        private readonly IMessageSerializer _messageSerializer;
        public const string Completed = "{completed}";

        public RedisPubSub(IConnectionMultiplexer connection, IMessageSerializer messageSerializer)
        {
            _connection = connection;
            _messageSerializer = messageSerializer;
        }

        public async ValueTask SendAsync<TTopic, TMessage>(
            TTopic topic,
            TMessage message,
            CancellationToken cancellationToken = default)
            where TTopic : notnull
        {
            ISubscriber subscriber = _connection.GetSubscriber();
            var serializedTopic = topic is string s ? s : _messageSerializer.Serialize(topic);
            var serializedMessage = _messageSerializer.Serialize(message);
            await subscriber.PublishAsync(serializedTopic, serializedMessage).ConfigureAwait(false);
        }

        public async ValueTask CompleteAsync<TTopic>(TTopic topic)
            where TTopic : notnull
        {
            ISubscriber subscriber = _connection.GetSubscriber();
            string serializedTopic = topic is string s ? s : _messageSerializer.Serialize(topic);
            await subscriber.PublishAsync(serializedTopic, Completed).ConfigureAwait(false);
        }

        public async ValueTask<IEventStream<TMessage>> SubscribeAsync<TTopic, TMessage>(
            TTopic topic,
            CancellationToken cancellationToken = default)
            where TTopic : notnull
        {
            ISubscriber subscriber = _connection.GetSubscriber();
            string serializedTopic = topic is string s ? s : _messageSerializer.Serialize(topic);

            ChannelMessageQueue channel = await subscriber
                .SubscribeAsync(serializedTopic)
                .ConfigureAwait(false);

            return new RedisEventStream<TMessage>(channel, _messageSerializer);
        }
    }
}
