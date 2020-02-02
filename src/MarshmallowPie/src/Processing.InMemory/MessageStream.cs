using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MarshmallowPie.Processing.InMemory
{
    internal class MessageStream<TMessage>
        : IMessageStream<TMessage>
        where TMessage : class
    {
        private readonly ChannelReader<TMessage> _channelReader;

        public MessageStream(ChannelReader<TMessage> channelReader)
        {
            _channelReader = channelReader;
        }

        public async IAsyncEnumerator<TMessage?> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            await foreach (TMessage message in _channelReader.ReadAllAsync(cancellationToken))
            {
                yield return message;
            }
        }

        public ValueTask CompleteAsync() => default;
    }
}
