using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Subscriptions;
using StackExchange.Redis;

namespace Subscriptions.Redis
{
    public class RedisEventStream : IEventStream
    {
        private readonly ChannelMessageQueue _channel;

        public RedisEventStream(ChannelMessageQueue channel)
        {
            _channel = channel;
        }

        public bool IsCompleted => _channel.Channel.IsNullOrEmpty;

        public async Task<IEventMessage> ReadAsync()
        {
            ChannelMessage message = await _channel
                .ReadAsync();
            
            return new EventMessage(
                message.Channel, message.Message.Box());
        }

        public async Task<IEventMessage> ReadAsync(
            CancellationToken cancellationToken)
        {
            ChannelMessage message = await _channel
                .ReadAsync(cancellationToken);

            return new EventMessage(
                message.Channel, message.Message.Box());
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
