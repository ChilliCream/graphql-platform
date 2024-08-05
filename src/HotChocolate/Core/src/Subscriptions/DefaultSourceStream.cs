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
    private readonly DefaultTopic<TMessage> _topic;
    private readonly Channel<TMessage> _channel;

    internal DefaultSourceStream(DefaultTopic<TMessage> topic, Channel<TMessage> channel)
    {
        _topic = topic;
        _channel = channel;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TMessage> ReadEventsAsync()
        => new MessageEnumerable(_channel.Reader);

    /// <inheritdoc />
    IAsyncEnumerable<object?> ISourceStream.ReadEventsAsync()
        => new MessageEnumerableAsObject(_channel.Reader);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        // if the source stream is disposed, we are completing the channel which will trigger
        // an unsubscribe from the topic.
        _channel.Writer.TryComplete();
        _topic.Unsubscribe(_channel);
        return default;
    }

    private sealed class MessageEnumerable : IAsyncEnumerable<TMessage>
    {
        private readonly ChannelReader<TMessage> _reader;

        public MessageEnumerable(ChannelReader<TMessage> reader)
        {
            _reader = reader;
        }

        public IAsyncEnumerator<TMessage> GetAsyncEnumerator(
            CancellationToken cancellationToken)
            => _reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
    }

    private sealed class MessageEnumerableAsObject : IAsyncEnumerable<object?>
    {
        private readonly ChannelReader<TMessage> _reader;

        public MessageEnumerableAsObject(ChannelReader<TMessage> reader)
        {
            _reader = reader;
        }

        public IAsyncEnumerator<object?> GetAsyncEnumerator(
            CancellationToken cancellationToken)
            => new MessageEnumeratorAsObject(
                _reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken));
    }

    private sealed class MessageEnumeratorAsObject : IAsyncEnumerator<object?>
    {
        private readonly IAsyncEnumerator<TMessage> _enumerator;

        public MessageEnumeratorAsObject(IAsyncEnumerator<TMessage> enumerator)
        {
            _enumerator = enumerator;
        }

        public object? Current => _enumerator.Current;

        public async ValueTask<bool> MoveNextAsync()
            => await _enumerator.MoveNextAsync().ConfigureAwait(false);

        public ValueTask DisposeAsync()
            => _enumerator.DisposeAsync();
    }
}
