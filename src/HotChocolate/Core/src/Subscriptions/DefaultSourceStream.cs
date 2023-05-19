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
    private readonly TaskCompletionSource<bool> _completed = new();
    private readonly TopicShard<TMessage> _shard;
    private readonly Channel<TMessage> _outgoing;

    internal DefaultSourceStream(TopicShard<TMessage> shard, Channel<TMessage> outgoing)
    {
        _shard = shard ?? throw new ArgumentNullException(nameof(shard));
        _outgoing = outgoing ?? throw new ArgumentNullException(nameof(outgoing));
    }

    internal Channel<TMessage> Outgoing => _outgoing;

    internal void Complete() => _completed.TrySetResult(true);

    /// <inheritdoc />
    public IAsyncEnumerable<TMessage> ReadEventsAsync()
        => new MessageEnumerable(_outgoing.Reader, _completed);

    /// <inheritdoc />
    IAsyncEnumerable<object?> ISourceStream.ReadEventsAsync()
        => new MessageEnumerableAsObject(_outgoing.Reader, _completed);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        // if the source stream is disposed, we are completing the channel which will trigger
        // an unsubscribe from the topic.
        _outgoing.Writer.TryComplete();
        _shard.Unsubscribe(this);
        return default;
    }

    private sealed class MessageEnumerable : IAsyncEnumerable<TMessage>
    {
        private readonly ChannelReader<TMessage> _reader;
        private readonly TaskCompletionSource<bool> _completed;

        public MessageEnumerable(
            ChannelReader<TMessage> reader,
            TaskCompletionSource<bool> completed)
        {
            _reader = reader;
            _completed = completed;
        }

        public IAsyncEnumerator<TMessage> GetAsyncEnumerator(
            CancellationToken cancellationToken)
            => new MessageEnumerator(_reader, _completed, cancellationToken);
    }

    private sealed class MessageEnumerator : IAsyncEnumerator<TMessage>
    {
        private readonly ChannelReader<TMessage> _reader;
        private readonly TaskCompletionSource<bool> _completed;
        private readonly CancellationToken _cancellationToken;

        public MessageEnumerator(
            ChannelReader<TMessage> reader,
            TaskCompletionSource<bool> completed,
            CancellationToken cancellationToken)
        {
            _reader = reader;
            _completed = completed;
            _cancellationToken = cancellationToken;
        }

        public TMessage Current { get; private set; } = default!;

        public async ValueTask<bool> MoveNextAsync()
        {
            try
            {
                while (!_reader.Completion.IsCompleted)
                {
                    if (_reader.TryRead(out var message))
                    {
                        Current = message;
                        return true;
                    }

                    if (_completed.Task.IsCompleted)
                    {
                        break;
                    }

                    await Task.WhenAny(_completed.Task, WaitForMessages())
                        .ConfigureAwait(false);

                    if (_completed.Task.IsCompleted && !_reader.TryPeek(out _))
                    {
                        break;
                    }
                }
            }
            catch
            {
                // ignore errors
            }

            return false;

            async Task WaitForMessages()
                => await _reader.WaitToReadAsync(_cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            _completed.TrySetCanceled();
            return default;
        }
    }

    private sealed class MessageEnumerableAsObject : IAsyncEnumerable<object?>
    {
        private readonly ChannelReader<TMessage> _reader;
        private readonly TaskCompletionSource<bool> _completed;

        public MessageEnumerableAsObject(
            ChannelReader<TMessage> reader,
            TaskCompletionSource<bool> completed)
        {
            _reader = reader;
            _completed = completed;
        }

        public IAsyncEnumerator<object?> GetAsyncEnumerator(
            CancellationToken cancellationToken)
            => new MessageEnumeratorAsObject(_reader, _completed, cancellationToken);
    }

    private sealed class MessageEnumeratorAsObject : IAsyncEnumerator<object?>
    {
        private readonly ChannelReader<TMessage> _reader;
        private readonly TaskCompletionSource<bool> _completed;
        private readonly CancellationToken _cancellationToken;

        public MessageEnumeratorAsObject(
            ChannelReader<TMessage> reader,
            TaskCompletionSource<bool> completed,
            CancellationToken cancellationToken)
        {
            _reader = reader;
            _completed = completed;
            _cancellationToken = cancellationToken;
        }

        public object? Current { get; private set; }

        public async ValueTask<bool> MoveNextAsync()
        {
            try
            {
                while (!_reader.Completion.IsCompleted)
                {
                    if (_reader.TryRead(out var message))
                    {
                        Current = message;
                        return true;
                    }

                    if (_completed.Task.IsCompleted)
                    {
                        break;
                    }

                    await Task.WhenAny(_completed.Task, WaitForMessages())
                        .ConfigureAwait(false);

                    if (_completed.Task.IsCompleted && !_reader.TryPeek(out _))
                    {
                        break;
                    }
                }
            }
            catch
            {
                // ignore errors
            }

            return false;

            async Task WaitForMessages()
                => await _reader.WaitToReadAsync(_cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            _completed.TrySetCanceled();
            return default;
        }
    }
}
