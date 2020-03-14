using System.Text.Json;
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
        public const string Completed = "{completed}";

        public RedisPubSub(IConnectionMultiplexer connection)
        {
            _connection = connection;
        }

        public async ValueTask SendAsync<TTopic, TMessage>(
            TTopic topic,
            TMessage message,
            CancellationToken cancellationToken = default)
            where TTopic : notnull
        {
            ISubscriber subscriber = _connection.GetSubscriber();
            string serializedTopic = topic is string s ? s : JsonSerializer.Serialize(topic);
            string serializedMessage = JsonSerializer.Serialize(message);
            await subscriber.PublishAsync(serializedTopic, serializedMessage).ConfigureAwait(false);
        }

        public async ValueTask CompleteAsync<TTopic>(TTopic topic)
            where TTopic : notnull
        {
            ISubscriber subscriber = _connection.GetSubscriber();
            string serializedTopic = topic is string s ? s : JsonSerializer.Serialize(topic);
            await subscriber.PublishAsync(serializedTopic, Completed).ConfigureAwait(false);
        }

        public async ValueTask<IEventStream<TMessage>> SubscribeAsync<TTopic, TMessage>(
            TTopic topic,
            CancellationToken cancellationToken = default)
            where TTopic : notnull
        {
            ISubscriber subscriber = _connection.GetSubscriber();
            string serializedTopic = topic is string s ? s : JsonSerializer.Serialize(topic);

            ChannelMessageQueue channel = await subscriber
                .SubscribeAsync(serializedTopic)
                .ConfigureAwait(false);

            return new RedisEventStream<TMessage>(channel);
        }
    }
}
