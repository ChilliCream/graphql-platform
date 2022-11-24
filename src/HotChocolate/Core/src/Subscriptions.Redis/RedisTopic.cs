using System.Threading.Channels;
using StackExchange.Redis;

namespace HotChocolate.Subscriptions.Redis;

internal sealed class RedisTopic<TMessage> : DefaultTopic<RedisMessageEnvelope<TMessage>, TMessage>
{
    private readonly ISubscriber _subscriber;
    private readonly IMessageSerializer _serializer;

    public RedisTopic(
        string name,
        ISubscriber subscriber,
        IMessageSerializer serializer,
        int capacity,
        TopicBufferFullMode fullMode)
        : base(name, capacity, fullMode)
    {
        _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    protected override async ValueTask<IDisposable> ConnectAsync(
        ChannelWriter<RedisMessageEnvelope<TMessage>> incoming)
    {
        var messageQueue = await _subscriber.SubscribeAsync(Name);

        messageQueue.OnMessage(async redisMessage =>
        {
            var rawMessage = redisMessage.Message.ToString();
            var envelope =_serializer.Deserialize<RedisMessageEnvelope<TMessage>>(rawMessage);
            await incoming.WriteAsync(envelope).ConfigureAwait(false);
        });

        return new Session(_subscriber, Name);
    }

    private sealed class Session : IDisposable
    {
        private readonly ISubscriber _subscriber;
        private readonly string _name;
        private bool _disposed;

        public Session(ISubscriber subscriber, string name)
        {
            _subscriber = subscriber;
            _name = name;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _subscriber.Unsubscribe(_name);
                _disposed = true;
            }
        }
    }
}
