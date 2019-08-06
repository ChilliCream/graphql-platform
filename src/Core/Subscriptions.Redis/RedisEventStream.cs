using System;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace HotChocolate.Subscriptions.Redis
{
    public class RedisEventStream
        : IEventStream
    {
        private readonly IEventDescription _eventDescription;
        private readonly ChannelMessageQueue _channel;
        private readonly IPayloadSerializer _serializer;
        private bool _isCompleted;

        public RedisEventStream(
            IEventDescription eventDescription,
            ChannelMessageQueue channel,
            IPayloadSerializer serializer)
        {
            _eventDescription = eventDescription;
            _channel = channel;
            _serializer = serializer;
        }

        public bool IsCompleted => _isCompleted;

        public Task<IEventMessage> ReadAsync() =>
            ReadAsync(CancellationToken.None);

        public async Task<IEventMessage> ReadAsync(
            CancellationToken cancellationToken)
        {
            ChannelMessage message = await _channel
                .ReadAsync(cancellationToken);

            var payload = _serializer.Deserialize(message.Message);

            return new EventMessage(message.Channel, payload);
        }

        public async Task CompleteAsync()
        {
            if (!_isCompleted)
            {
                await _channel.UnsubscribeAsync()
                    .ConfigureAwait(false);
                _isCompleted = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (!_isCompleted)
            {
                if (disposing)
                {
                    _channel.Unsubscribe();
                }
                _isCompleted = true;
            }
        }
    }
}
