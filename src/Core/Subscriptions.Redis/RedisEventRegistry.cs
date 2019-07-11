using System.Threading.Tasks;
using HotChocolate.Subscriptions;
using StackExchange.Redis;

namespace Subscriptions.Redis
{
    public class RedisEventRegistry
        : IEventRegistry
        , IEventSender
    {
        private readonly IPayloadSerializer _serializer;
        private readonly IConnectionMultiplexer _redis;

        public RedisEventRegistry(
            IPayloadSerializer serializer)
        {
            _serializer = serializer;
            // TODO: Add Options
            _redis = ConnectionMultiplexer.Connect(
                new ConfigurationOptions
                {
                    AbortOnConnectFail = false,
                    ConnectRetry = 3,
                    EndPoints = {"localhost:6379"}
                });
        }

        public async Task<IEventStream> SubscribeAsync(
            IEventDescription eventDescription)
        {
            ISubscriber subscriber = _redis.GetSubscriber();
            ChannelMessageQueue channel = await subscriber
                .SubscribeAsync(eventDescription.ToString());
            
            return new RedisEventStream(channel, _serializer);
        }

        public async Task SendAsync(IEventMessage message)
        {
            ISubscriber subscriber = _redis.GetSubscriber();

            byte[] payload = await _serializer
                .SerializeAsync(message.Payload);

            await subscriber
                .PublishAsync(message.Event.ToString(), payload);
        }
    }
}
