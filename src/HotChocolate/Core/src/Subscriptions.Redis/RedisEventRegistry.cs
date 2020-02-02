using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace HotChocolate.Subscriptions.Redis
{
    public class RedisEventRegistry
        : IEventRegistry
        , IEventSender
    {
        private readonly IConnectionMultiplexer _connection;
        private readonly IPayloadSerializer _serializer;

        public RedisEventRegistry(
            IConnectionMultiplexer connection,
            IPayloadSerializer serializer)
        {
            _connection = connection;
            _serializer = serializer;
        }

        public async ValueTask<IEventStream> SubscribeAsync(
            IEventDescription eventDescription,
            CancellationToken cancellationToken = default)
        {
            ISubscriber subscriber = _connection.GetSubscriber();

            ChannelMessageQueue channel = await subscriber
                .SubscribeAsync(eventDescription.ToString())
                .ConfigureAwait(false);

            return new RedisEventStream(eventDescription, channel, _serializer);
        }

        public async ValueTask SendAsync(
            IEventMessage message,
            CancellationToken cancellationToken = default)
        {
            ISubscriber subscriber = _connection
                .GetSubscriber();

            string channel = message.Event.ToString();
            byte[] payload = _serializer.Serialize(message.Payload);

            await subscriber.PublishAsync(channel, payload)
                .ConfigureAwait(false);
        }
    }
}
