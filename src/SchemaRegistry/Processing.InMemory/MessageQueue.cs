using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MarshmallowPie.Processing.InMemory
{
    public class MessageQueue<TMessage>
        : IMessageSender<TMessage>
        , IMessageReceiver<TMessage>
    {
        private readonly Channel<TMessage> _channel = Channel.CreateUnbounded<TMessage>();

        public ValueTask SendAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(message, cancellationToken);
        }

        public ValueTask<IAsyncEnumerable<TMessage>> SubscribeAsync(CancellationToken cancellationToken = default)
        {
            return new ValueTask<IAsyncEnumerable<TMessage>>(_channel.Reader.ReadAllAsync());
        }
    }
}
