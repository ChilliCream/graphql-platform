using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Subscriptions;
using StackExchange.Redis;

namespace Subscriptions.Redis
{
    public class RedisEventStream : IEventStream
    {
        private readonly ChannelMessageQueue _channel;
        private readonly IPayloadSerializer _serializer;

        public RedisEventStream(
            ChannelMessageQueue channel,
            IPayloadSerializer serializer)
        {
            _channel = channel;
            _serializer = serializer;
        }

        public bool IsCompleted => _channel.Channel.IsNullOrEmpty;

        public async Task<IEventMessage> ReadAsync()
        {
            ChannelMessage message = await _channel
                .ReadAsync();

            var payload = await _serializer
                .DeserializeAsync(message.Message);
            
            return new EventMessage(
                message.Channel, payload);
        }

        public async Task<IEventMessage> ReadAsync(
            CancellationToken cancellationToken)
        {
            ChannelMessage message = await _channel
                .ReadAsync(cancellationToken);

            var payload = await _serializer
                .DeserializeAsync(message.Message);

            return new EventMessage(
                message.Channel, payload);
        }

        public async Task CompleteAsync()
        {
            await _channel.Completion;
        }

        public void Dispose()
        {
            _channel.Unsubscribe();
        }
    }
}
