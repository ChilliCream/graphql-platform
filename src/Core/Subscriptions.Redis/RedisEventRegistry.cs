using System.Threading.Tasks;
using HotChocolate.Subscriptions;
using StackExchange.Redis;

namespace Subscriptions.Redis
{
    public class RedisEventRegistry
        : IEventRegistry
        , IEventSender
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisEventRegistry()
        {
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
                .SubscribeAsync(eventDescription.Name);
            
            return new RedisEventStream(channel);
        }

        public async Task SendAsync(IEventMessage message)
        {
            ISubscriber subscriber = _redis.GetSubscriber();

            await subscriber.PublishAsync(
                message.Event.Name,
                RedisValue.Unbox(message.Payload)); // TODO: Review EventMessage
        }
    }
}
