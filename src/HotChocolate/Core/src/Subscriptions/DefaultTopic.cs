using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Channel<TMessage> _incoming;
    private readonly List<TopicShard<TMessage>> _shards = new();
    private readonly BoundedChannelOptions _channelOptions;
    private readonly ISubscriptionDiagnosticEvents _diagnosticEvents;
    private int _subscribers;
    private bool _completed;
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
        _incoming = CreateUnbounded<TMessage>();
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

    /// <summary>
    /// Publishes a new message to this topic.
    /// </summary>
    /// <param name="message">
    /// The message that shall be published.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous publish operation.
    /// </returns>
    public ValueTask PublishAsync(TMessage message, CancellationToken cancellationToken = default)
    {
        if (_completed)
        {
            return ValueTask.CompletedTask;
        }

        return _incoming.Writer.WriteAsync(message, cancellationToken);
    }

    public void TryComplete()
    {
        var raiseEvent = false;

        _lock.EnterWriteLock();

        try
        {
            if (_completed == false)
            {
                raiseEvent = true;
            }

            _completed = true;
            _incoming.Writer.TryComplete();
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        if (raiseEvent)
        {
            RaiseClosedEvent();
        }
    }

    internal ISourceStream<TMessage>? TrySubscribe()
    {
        try
        {
            _lock.EnterWriteLock();

            try
            {
                if (_completed)
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
                _lock.ExitWriteLock();
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
            while (await _incoming.Reader.WaitToReadAsync(ct).ConfigureAwait(false))
            {
                DispatchToShards();
            }
        }
        finally
        {
            CompleteShards();
            Dispose();
        }
    }

    private void DispatchToShards()
    {
        var reads = 0;
        _lock.EnterReadLock();

        try
        {
            while (_incoming.Reader.TryRead(out var message))
            {
                _diagnosticEvents.Dispatch(Name, message, _subscribers);

                var shardsSpan = AsSpan(_shards);
                ref var start = ref MemoryMarshal.GetReference(shardsSpan);
                ref var end = ref Unsafe.Add(ref start, shardsSpan.Length);

                while (Unsafe.IsAddressLessThan(ref start, ref end))
                {
                    start.Writer.TryWrite(message);
                    start = ref Unsafe.Add(ref start, 1);
                }

                if (++reads > 5)
                {
                    break;
                }
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private void CompleteShards()
    {
        _lock.EnterReadLock();

        _incoming.Writer.TryComplete();

        try
        {
            var shardsSpan = AsSpan(_shards);
            ref var start = ref MemoryMarshal.GetReference(shardsSpan);
            ref var end = ref Unsafe.Add(ref start, shardsSpan.Length);

            while (Unsafe.IsAddressLessThan(ref start, ref end))
            {
                start.Writer.Complete();
                start = ref Unsafe.Add(ref start, 1);
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Override this method to connect to an external pub/sub provider.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns a session to dispose the subscription session.
    /// </returns>
    protected virtual ValueTask<IDisposable> OnConnectAsync(
        CancellationToken cancellationToken)
        => new(DefaultSession.Instance);

    private TopicShard<TMessage> CreateShard(int shardId)
    {
        var shard = new TopicShard<TMessage>(_channelOptions, Name, shardId, DiagnosticEvents);

        shard.Unsubscribed += subscribers =>
        {
            if (_completed)
            {
                return;
            }

            var raise = false;

            _lock.EnterWriteLock();

            try
            {
                _subscribers -= subscribers;

                if (_subscribers == 0 && !_completed)
                {
                    _completed = true;
                    raise = true;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
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
                _cts.Dispose();
                _shards.Clear();
            }

            _completed = true;
            _disposed = true;
        }
    }

    private sealed class DefaultSession : IDisposable
    {
        private DefaultSession() { }
        public void Dispose() { }

        public static readonly DefaultSession Instance = new();
    }
}
