using System.Threading.Channels;
using HotChocolate.Execution;

namespace HotChocolate.Subscriptions.Nats;

/// <summary>
/// Represents the NATS event stream.
/// </summary>
/// <typeparam name="TMessage">
/// The message type.
/// </typeparam>
internal sealed class NatsSourceStream<TMessage> : ISourceStream<TMessage>
{
    private readonly Channel<EventMessageEnvelope<TMessage>> _channel;

    /// <summary>
    /// Initializes a new instance of <see cref="TMessage"/>.
    /// </summary>
    /// <param name="channel">
    /// The internal message channel.
    /// </param>
    public NatsSourceStream(Channel<EventMessageEnvelope<TMessage>> channel)
        => _channel = channel;

    /// <inheritdoc />
    public IAsyncEnumerable<TMessage> ReadEventsAsync()
        => new NatsAsyncEnumerable(_channel.Reader);

    /// <inheritdoc />
    IAsyncEnumerable<object> ISourceStream.ReadEventsAsync()
        => new NatsAsyncEnumerableAsObject(_channel.Reader);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        // if the source stream is disposed, we are completing the channel which will trigger
        // an unsubscribe from the topic.
        _channel.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }

    private sealed class NatsAsyncEnumerable : IAsyncEnumerable<TMessage>
    {
        private readonly ChannelReader<EventMessageEnvelope<TMessage>> _reader;

        public NatsAsyncEnumerable(ChannelReader<EventMessageEnvelope<TMessage>> reader)
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

    private sealed class NatsAsyncEnumerableAsObject : IAsyncEnumerable<object>
    {
        private readonly ChannelReader<EventMessageEnvelope<TMessage>> _reader;

        public NatsAsyncEnumerableAsObject(ChannelReader<EventMessageEnvelope<TMessage>> reader)
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
