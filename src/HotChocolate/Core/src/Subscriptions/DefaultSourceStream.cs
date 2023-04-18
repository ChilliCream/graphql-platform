using System.Threading.Channels;
using HotChocolate.Execution;

namespace HotChocolate.Subscriptions;

/// <summary>
/// Represents the default source stream implementation.
/// </summary>
/// <typeparam name="TMessage">
/// The message type.
/// </typeparam>
internal sealed class DefaultSourceStream<TMessage> : ISourceStream<TMessage>
{
    private readonly TopicShard<TMessage> _shard;
    private readonly Channel<TMessage> _outgoing;

    internal DefaultSourceStream(TopicShard<TMessage> shard, Channel<TMessage> outgoing)
    {
        _shard = shard ?? throw new ArgumentNullException(nameof(shard));
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
        _shard.Unsubscribe(_outgoing);
        return default;
    }

    private sealed class MessageEnumerable : IAsyncEnumerable<TMessage>
    {
        private readonly ChannelReader<TMessage> _reader;

        public MessageEnumerable(ChannelReader<TMessage> reader)
            => _reader = reader;

        public IAsyncEnumerator<TMessage> GetAsyncEnumerator(CancellationToken cancellationToken)
            => _reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
    }

    private sealed class MessageEnumerableAsObject : IAsyncEnumerable<object>
    {
        private readonly ChannelReader<TMessage> _reader;

        public MessageEnumerableAsObject(ChannelReader<TMessage> reader)
            => _reader = reader;

        public async IAsyncEnumerator<object> GetAsyncEnumerator(
            CancellationToken cancellationToken)
        {
            await foreach(var message in _reader.ReadAllAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                yield return message!;
            }
        }
    }
}
