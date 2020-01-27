using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MarshmallowPie.Processing.InMemory
{
    public class SessionMessageQueue<TMessage>
        : IMessageSender<TMessage>
        , ISessionMessageReceiver<TMessage>
        where TMessage : ISessionMessage
    {
        private readonly ConcurrentDictionary<string, Channel<TMessage>> _sessions =
            new ConcurrentDictionary<string, Channel<TMessage>>();

        public ValueTask SendAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            Channel<TMessage> channel = _sessions.GetOrAdd(message.SessionId, s => Channel.CreateUnbounded<TMessage>());
            return channel.Writer.WriteAsync(message, cancellationToken);
        }

        public ValueTask<IAsyncEnumerable<TMessage>> SubscribeAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            Channel<TMessage> channel = _sessions.GetOrAdd(sessionId, s => Channel.CreateUnbounded<TMessage>());
            return new ValueTask<IAsyncEnumerable<TMessage>>(GetMessagesAsync(sessionId, channel));
        }

        private async IAsyncEnumerable<TMessage> GetMessagesAsync(string sessionId, Channel<TMessage> channel)
        {
            await foreach (TMessage message in channel.Reader.ReadAllAsync())
            {
                yield return message;

                if (message.IsCompleted)
                {
                    break;
                }
            }

            _sessions.TryRemove(sessionId, out _);
            channel.Writer.Complete();
        }
    }
}
