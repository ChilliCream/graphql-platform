using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Subscriptions;

/// <summary>
/// Represents the NATS event stream.
/// </summary>
/// <typeparam name="TEnvelope">
/// The message envelope type.
/// </typeparam>
/// <typeparam name="TMessage">
/// The message type.
/// </typeparam>
public sealed class DefaultSourceStream<TEnvelope, TMessage>
    : ISourceStream<TMessage>
    where TEnvelope : DefaultMessageEnvelope<TMessage>
{
    private readonly Channel<TEnvelope> _channel;

    /// <summary>
    /// Initializes a new instance of <see cref="TMessage"/>.
    /// </summary>
    /// <param name="channel">
    /// The internal message channel.
    /// </param>
    public DefaultSourceStream(Channel<TEnvelope> channel)
        => _channel = channel;

    /// <inheritdoc />
    public IAsyncEnumerable<TMessage> ReadEventsAsync()
        => new MessageEnumerable(_channel.Reader);

    /// <inheritdoc />
    IAsyncEnumerable<object> ISourceStream.ReadEventsAsync()
        => new MessageEnumerableAsObject(_channel.Reader);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        // if the source stream is disposed, we are completing the channel which will trigger
        // an unsubscribe from the topic.
        _channel.Writer.TryComplete();
        return default;
    }

    private sealed class MessageEnumerable : IAsyncEnumerable<TMessage>
    {
        private readonly ChannelReader<TEnvelope> _reader;

        public MessageEnumerable(ChannelReader<TEnvelope> reader)
            => _reader = reader;

        public async IAsyncEnumerator<TMessage> GetAsyncEnumerator(
            CancellationToken cancellationToken)
        {
            while (await _reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_reader.TryRead(out var message))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (message.IsCompletedMessage)
                    {
                        yield break;
                    }

                    yield return message.Body;
                }
            }
        }
    }

    private sealed class MessageEnumerableAsObject : IAsyncEnumerable<object>
    {
        private readonly ChannelReader<TEnvelope> _reader;

        public MessageEnumerableAsObject(ChannelReader<TEnvelope> reader)
            => _reader = reader;

        public async IAsyncEnumerator<object> GetAsyncEnumerator(
            CancellationToken cancellationToken)
        {
            while (await _reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_reader.TryRead(out var message))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (message.IsCompletedMessage)
                    {
                        yield break;
                    }

                    yield return message.Body;
                }
            }
        }
    }
}
