using System.Diagnostics;
using System.Threading.Channels;
using HotChocolate.Execution;
using static HotChocolate.Subscriptions.Properties.Resources;

namespace HotChocolate.Subscriptions;

/// <summary>
/// Represents the default source stream implementation.
/// </summary>
/// <typeparam name="TMessage">
/// The message type.
/// </typeparam>
internal sealed class DefaultSourceStream<TMessage> : ISourceStream<TMessage>
{
    private readonly ChannelWriter<MessageEnvelope<TMessage>> _incoming;
    private readonly Channel<MessageEnvelope<TMessage>> _outgoing;

    internal DefaultSourceStream(
        ChannelWriter<MessageEnvelope<TMessage>> incoming,
        Channel<MessageEnvelope<TMessage>> outgoing)
    {
        _incoming = incoming ?? throw new ArgumentNullException(nameof(incoming));
        _outgoing = outgoing ?? throw new ArgumentNullException(nameof(outgoing));
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TMessage> ReadEventsAsync()
        => new MessageEnumerable(_outgoing.Reader);

    /// <inheritdoc />
    IAsyncEnumerable<object> ISourceStream.ReadEventsAsync()
        => new MessageEnumerableAsObject(_outgoing.Reader);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        // if the source stream is disposed, we are completing the channel which will trigger
        // an unsubscribe from the topic.
        _outgoing.Writer.TryComplete();
        _incoming.TryWrite(new MessageEnvelope<TMessage>(kind: MessageKind.Unsubscribed));
        return default;
    }

    private sealed class MessageEnumerable : IAsyncEnumerable<TMessage>
    {
        private readonly ChannelReader<MessageEnvelope<TMessage>> _reader;

        public MessageEnumerable(ChannelReader<MessageEnvelope<TMessage>> reader)
            => _reader = reader;

        public async IAsyncEnumerator<TMessage> GetAsyncEnumerator(
            CancellationToken cancellationToken)
        {
            while (await _reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_reader.TryRead(out var message))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    switch (message.Kind)
                    {
                        case MessageKind.Default:
                            // a default message must have a body
                            Debug.Assert(message.Body != null, "message.Body != null");
                            yield return message.Body!;
                            break;

                        case MessageKind.Completed:
                            // a complete message will cause the stream to complete.
                            yield break;

                        case MessageKind.Unsubscribed:
                            // these kind of messages should not arrive at the source stream.
                            throw new InvalidOperationException(
                                MessageEnumerable_UnsubscribedNotAllowed);

                        default:
                            throw new NotSupportedException();
                    }
                }
            }
        }
    }

    private sealed class MessageEnumerableAsObject : IAsyncEnumerable<object>
    {
        private readonly ChannelReader<MessageEnvelope<TMessage>> _reader;

        public MessageEnumerableAsObject(ChannelReader<MessageEnvelope<TMessage>> reader)
            => _reader = reader;

        public async IAsyncEnumerator<object> GetAsyncEnumerator(
            CancellationToken cancellationToken)
        {
            while (await _reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_reader.TryRead(out var message))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    switch (message.Kind)
                    {
                        case MessageKind.Default:
                            // a default message must have a body
                            Debug.Assert(message.Body != null, "message.Body != null");
                            yield return message.Body!;
                            break;

                        case MessageKind.Completed:
                            // a complete message will cause the stream to complete.
                            yield break;

                        case MessageKind.Unsubscribed:
                            // these kind of messages should not arrive at the source stream.
                            throw new InvalidOperationException(
                                MessageEnumerable_UnsubscribedNotAllowed);

                        default:
                            throw new NotSupportedException();
                    }
                }
            }
        }
    }
}
