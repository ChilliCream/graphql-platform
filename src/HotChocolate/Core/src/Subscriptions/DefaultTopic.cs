using System.Threading.Channels;
using HotChocolate.Execution;
using HotChocolate.Subscriptions.Diagnostics;
using static System.Runtime.InteropServices.CollectionsMarshal;
using static System.Threading.Channels.Channel;

namespace HotChocolate.Subscriptions;

/// <summary>
/// This base class can be used to implement a Hot Chocolate subscription provider and already
/// implements a lot of the logic needed for the typical pub/sub topic like backpressure/throttling.
/// </summary>
/// <typeparam name="TMessage">
/// The message.
/// </typeparam>
public abstract class DefaultTopic<TMessage> : ITopic
{
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly TaskCompletionSource<bool> _completion = new();
    private readonly Channel<TMessage> _incoming;
    private readonly List<TopicShard<TMessage>> _shards = new();
    private readonly BoundedChannelOptions _channelOptions;
    private readonly ISubscriptionDiagnosticEvents _diagnosticEvents;
    private int _subscribers;
    private bool _completed;
    private bool _closed;
    private bool _disposed;

    public event EventHandler<EventArgs>? Closed;

    protected DefaultTopic(
        string name,
        int capacity,
        TopicBufferFullMode fullMode,
        ISubscriptionDiagnosticEvents diagnosticEvents)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _channelOptions = new BoundedChannelOptions(capacity)
        {
            FullMode = (BoundedChannelFullMode)(int)fullMode
        };
        _incoming = CreateBounded<TMessage>(_channelOptions);
        _diagnosticEvents = diagnosticEvents;

        // initially we will run with one shard.
        _shards.Add(new TopicShard<TMessage>(_channelOptions, name, 0, diagnosticEvents));
    }

    protected DefaultTopic(
        string name,
        int capacity,
        TopicBufferFullMode fullMode,
        ISubscriptionDiagnosticEvents diagnosticEvents,
        Channel<TMessage> incomingMessages)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _channelOptions = new BoundedChannelOptions(capacity)
        {
            FullMode = (BoundedChannelFullMode)(int)fullMode
        };
        _incoming = incomingMessages;
        _diagnosticEvents = diagnosticEvents;

        // initially we will run with one shard.
        _shards.Add(new TopicShard<TMessage>(_channelOptions, name, 0, diagnosticEvents));
    }

    /// <summary>
    /// Gets the name of this topic.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the message type of this topic.
    /// </summary>
    public Type MessageType => typeof(TMessage);

    /// <summary>
    /// Allows access to the diagnostic events.
    /// </summary>
    protected ISubscriptionDiagnosticEvents DiagnosticEvents => _diagnosticEvents;

    public ValueTask PublishAsync(TMessage message, CancellationToken cancellationToken = default)
    {
        if(_completed || _closed || _disposed)
        {
            return ValueTask.CompletedTask;
        }

        return _incoming.Writer.WriteAsync(message, cancellationToken);
    }

    public void TryComplete()
    {
        _completed = true;
        _completion.TrySetResult(_completed);
    }

    internal async ValueTask<ISourceStream<TMessage>?> TrySubscribeAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_closed)
                {
                    // it could have happened that we have entered subscribe after it was
                    // already be completed. In this case we will return null to
                    // signal that subscribe was unsuccessful.
                    return null;
                }

                var stream = GetOptimalShard().Subscribe();
                _diagnosticEvents.SubscribeSuccess(Name);
                return stream;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch
        {
            // it could have happened that we entered subscribe at the moment dispose is hit,
            // in this case we return null to signal that subscribe was unsuccessful.
            return null;
        }
    }

    /// <summary>
    /// Gets the shard that is cheapest to get and is not considered full.
    ///
    /// We will try to balance the shards, but shards that have less than 250 subscribers
    /// will always be picked since such a shard already will perform optimally.
    /// </summary>
    private TopicShard<TMessage> GetOptimalShard()
    {
        var shards = AsSpan(_shards);

        var best = shards[0];

        // if the first shard has less than 250 subscribers we will just take it.
        if (best.Subscribers < 250)
        {
            return best;
        }

        if (shards.Length == 1)
        {
            // if the first shard has more than 250 subscribers we will scale.
            var second = CreateShard(1);
            _shards.Insert(0, second);
            return second;
        }

        for (var i = 1; i < shards.Length; i++)
        {
            var current = shards[i];

            if (best.Subscribers > current.Subscribers)
            {
                best = current;
            }

            if (best.Subscribers < 250)
            {
                return best;
            }
        }

        if (best.Subscribers > 999)
        {
            // new shards are always inserted as the first shard
            // so that we do not need to iterate to find it as the
            // best shard.
            best = CreateShard(shards.Length);
            _shards.Insert(0, best);
        }

        return best;
    }

    internal async ValueTask ConnectAsync(CancellationToken ct = default)
    {
        try
        {
            var session = await OnConnectAsync(ct).ConfigureAwait(false);
            DiagnosticEvents.Connected(Name);

            BeginProcessing(session);
        }
        catch (Exception ex)
        {
            DiagnosticEvents.MessageProcessingError(Name, ex);
        }
    }

    private void BeginProcessing(IDisposable session)
        => Task.Factory.StartNew(
            async s => await ProcessMessagesSessionAsync((IDisposable)s!).ConfigureAwait(false),
            session);

    private async Task ProcessMessagesSessionAsync(IDisposable session)
    {
        try
        {
            await ProcessMessagesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DiagnosticEvents.MessageProcessingError(Name, ex);
        }
        finally
        {
            session.Dispose();
            DiagnosticEvents.Disconnected(Name);
        }
    }

    private async Task ProcessMessagesAsync()
    {
        var ct = _cts.Token;

        try
        {
            var postponedMessages = new List<PostponedMessage>();

            while (!_closed && !_incoming.Reader.Completion.IsCompleted)
            {
                if (_incoming.Reader.TryPeek(out _))
                {
                    DispatchMessagesToShards(postponedMessages);
                    await DispatchDelayedMessagesAsync(postponedMessages).ConfigureAwait(false);
                }
                else
                {
                    if (_completed)
                    {
                        break;
                    }

                    _diagnosticEvents.WaitForMessages(Name);
                    await Task.WhenAny(_completion.Task, WaitForMessages(ct)).ConfigureAwait(false);

                    if (_completed && !_incoming.Reader.TryPeek(out _))
                    {
                        break;
                    }
                }
            }
        }
        finally
        {
            TryClose();
            Dispose();
        }
    }

    private async Task WaitForMessages(CancellationToken ct)
        => await _incoming.Reader.WaitToReadAsync(ct).ConfigureAwait(false);

    /// <summary>
    /// Override this method to connect to an external pub/sub provider.
    /// </summary>
    /// <param name="incoming">
    /// Write incoming messages to this channel writer.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns a session to dispose the subscription session.
    /// </returns>
    protected virtual ValueTask<IDisposable> OnConnectAsync(
        CancellationToken cancellationToken)
        => new(DefaultSession.Instance);

    private void DispatchMessagesToShards(List<PostponedMessage> postponedMessages)
    {
        var batchSize = 4;
        var dispatched = 0;
        var subscribers = _subscribers;

        while (_incoming.Reader.TryRead(out var message))
        {
            // we are not locking at this point since the only thing happening to this list
            // is that new shards are being added. Shards are never being removed from a topic.
            var shards = AsSpan(_shards);

            _diagnosticEvents.Dispatch(Name, message, _subscribers);

            for (var i = 0; i < shards.Length; i++)
            {
                var shard = shards[i];

                if (!shard.Writer.TryWrite(message))
                {
                    // if we cannot write because of back pressure we will postpone
                    // the message.
                    postponedMessages.Add(new PostponedMessage(message, shard));
                }
            }

            // we try to avoid full message processing cycles and keep on dispatching messages,
            // but we will interrupt every 4 messages and allow for new subscribers
            // to join in. Also, we will interrupt if the subscriber count has changed.
            if (++dispatched >= batchSize || subscribers != _subscribers)
            {
                break;
            }
        }
    }

    private static async ValueTask DispatchDelayedMessagesAsync(
        List<PostponedMessage> postponedMessages)
    {
        if (postponedMessages.Count > 0)
        {
            for (var i = 0; i < postponedMessages.Count; i++)
            {
                var postponedMessage = postponedMessages[i];
                var shard = postponedMessage.Shard;
                var message = postponedMessage.Message;

                try
                {
                    await shard.Writer.WriteAsync(message).ConfigureAwait(false);
                }
                catch (ChannelClosedException)
                {
                    // the channel might have been closed in the meantime.
                    // we will skip over this error.
                    // It might signal that the shard was already disposed.
                }
            }
            postponedMessages.Clear();
        }
    }

    private void TryClose()
    {
        if (!_closed)
        {
            var raise = false;

            _semaphore.Wait();

            if (!_closed)
            {
                _closed = true;
                raise = true;
            }

            _semaphore.Release();

            if (raise)
            {
                RaiseClosedEvent();
            }
        }
    }

    private TopicShard<TMessage> CreateShard(int shardId)
    {
        var shard = new TopicShard<TMessage>(_channelOptions, Name, shardId, DiagnosticEvents);

        shard.Unsubscribed += subscribers =>
        {
            if (_closed)
            {
                return;
            }

            var raise = false;

            _semaphore.Wait();

            try
            {
                _subscribers -= subscribers;

                if (_subscribers == 0 && !_closed)
                {
                    _closed = true;
                    raise = true;
                }
            }
            finally
            {
                _semaphore.Release();
            }

            if (raise)
            {
                RaiseClosedEvent();
            }
        };

        return shard;
    }

    private void RaiseClosedEvent()
        => Closed?.Invoke(this, EventArgs.Empty);

    ~DefaultTopic()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _diagnosticEvents.Close(Name);
                _cts.Cancel();

                _incoming.Writer.TryComplete();

                foreach (var shard in _shards)
                {
                    shard.Writer.Complete();
                }

                _semaphore.Dispose();
                _cts.Dispose();
                _shards.Clear();
            }

            _closed = true;
            _disposed = true;
        }
    }

    private sealed class PostponedMessage
    {
        public PostponedMessage(
            TMessage message,
            TopicShard<TMessage> shard)
        {
            Message = message;
            Shard = shard;
        }

        public TMessage Message { get; }

        public TopicShard<TMessage> Shard { get; }
    }

    private sealed class DefaultSession : IDisposable
    {
        private DefaultSession() { }
        public void Dispose() { }

        public static readonly DefaultSession Instance = new();
    }
}
