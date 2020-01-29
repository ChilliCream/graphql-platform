using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MarshmallowPie.Processing.InMemory
{
    public class MessageQueue<TMessage>
        : IMessageSender<TMessage>
        , IMessageReceiver<TMessage>
        where TMessage : class
    {
        private readonly Channel<TMessage> _channel = Channel.CreateUnbounded<TMessage>();

        public ValueTask SendAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(message, cancellationToken);
        }

        public ValueTask<IMessageStream<TMessage>> SubscribeAsync(
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<IMessageStream<TMessage>>(
                new MessageStream<TMessage>(_channel.Reader));
        }
    }
}
