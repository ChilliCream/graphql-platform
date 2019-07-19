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

        public async Task<IEventStream> SubscribeAsync(
            IEventDescription eventDescription)
        {
            ISubscriber subscriber = _connection
                .GetSubscriber();

            ChannelMessageQueue channel = await subscriber
                .SubscribeAsync(eventDescription.ToString());

            return new RedisEventStream(channel, _serializer);
        }

        public async Task SendAsync(IEventMessage message)
        {
            ISubscriber subscriber = _connection
                .GetSubscriber();

            byte[] payload = await _serializer
                .SerializeAsync(message.Payload);

            await subscriber
                .PublishAsync(message.Event.ToString(), payload);
        }
    }
}
