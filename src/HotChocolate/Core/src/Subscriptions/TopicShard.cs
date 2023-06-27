using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using HotChocolate.Execution;
using HotChocolate.Subscriptions.Diagnostics;
using static System.Runtime.InteropServices.CollectionsMarshal;

namespace HotChocolate.Subscriptions;

internal sealed class TopicShard<TMessage>
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Channel<TMessage> _incoming;
    private readonly List<DefaultSourceStream<TMessage>> _subscribers = new();
    private readonly BoundedChannelOptions _channelOptions;
    private readonly ISubscriptionDiagnosticEvents _diagnosticEvents;
    private readonly string _topicName;
    private readonly int _topicShard;

    public TopicShard(
        BoundedChannelOptions channelOptions,
        string topicName,
        int topicShard,
        ISubscriptionDiagnosticEvents diagnosticEvents)
    {
        _channelOptions = channelOptions;
        _topicName = topicName;
        _topicShard = topicShard;
        _diagnosticEvents = diagnosticEvents;

        _incoming = Channel.CreateUnbounded<TMessage>();

        BeginProcessing();
    }

    public event Action<int>? Unsubscribed;

    /// <summary>
    /// Gets the count of current subscribers.
    /// </summary>
    public int Subscribers => _subscribers.Count;

    public ChannelWriter<TMessage> Writer => _incoming.Writer;

    public ISourceStream<TMessage> Subscribe()
    {
        var channel = Channel.CreateBounded<TMessage>(_channelOptions);
        var stream = new DefaultSourceStream<TMessage>(this, channel);

        _lock.EnterWriteLock();

        _subscribers.Add(stream);

        _lock.ExitWriteLock();

        return stream;
    }

    public void Unsubscribe(DefaultSourceStream<TMessage> channel)
    {
        _diagnosticEvents.Unsubscribe(_topicName, _topicShard, 1);

        _lock.EnterWriteLock();

        try
        {
            if (_subscribers.Remove(channel))
            {
                Unsubscribed?.Invoke(1);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void BeginProcessing()
        => Task.Factory.StartNew(
            async () => await ProcessMessagesAsync().ConfigureAwait(false));

    private async ValueTask ProcessMessagesAsync()
    {
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        try
        {
            while (await _incoming.Reader.WaitToReadAsync(ct).ConfigureAwait(false))
            {
                DispatchToSubscribers();
            }
        }
        finally
        {
            CompleteSubscribers();
            cts.Cancel();
        }
    }

    private void DispatchToSubscribers()
    {
        var reads = 0;
        _lock.EnterReadLock();

        while (_incoming.Reader.TryRead(out var message))
        {
            var subscribersSpan = AsSpan(_subscribers);
            ref var start = ref MemoryMarshal.GetReference(subscribersSpan);
            ref var end = ref Unsafe.Add(ref start, subscribersSpan.Length);

            while (Unsafe.IsAddressLessThan(ref start, ref end))
            {
                start.Write(message);
                start = ref Unsafe.Add(ref start, 1);
            }

            if (++reads > 5)
            {
                break;
            }
        }

        _lock.ExitReadLock();
    }

    private void CompleteSubscribers()
    {
        _incoming.Writer.TryComplete();

        _lock.EnterReadLock();

        try
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber.Complete();
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}
