using System.Threading.Channels;
using HotChocolate.Execution;
using static System.Runtime.InteropServices.CollectionsMarshal;
using static System.Threading.Channels.Channel;

namespace HotChocolate.Subscriptions;

/// <summary>
/// This base class can be used to implement a Hot Chocolate subscription provider and already
/// implements a lot of the logic needed for the typical pub/sub topic like backpressure/throttling.
/// </summary>
/// <typeparam name="TEnvelope">
/// The message envelope.
/// </typeparam>
/// <typeparam name="TMessage">
/// The message.
/// </typeparam>
public abstract class DefaultTopic<TEnvelope, TMessage>
    : IDisposable
    where TEnvelope : DefaultMessageEnvelope<TMessage>
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Channel<TEnvelope> _incoming;
    private readonly List<Channel<TEnvelope>> _outgoing = new();
    private readonly BoundedChannelOptions _channelOptions;
    private bool _closed;
    private bool _disposed;

    public event EventHandler<EventArgs>? Unsubscribed;

    protected DefaultTopic(
        string name,
        int capacity,
        TopicBufferFullMode fullMode)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _channelOptions = new BoundedChannelOptions(capacity)
        {
            FullMode = (BoundedChannelFullMode)(int)fullMode
        };
        _incoming = CreateBounded<TEnvelope>(_channelOptions);
        BeginProcessing();
    }

    protected DefaultTopic(
        string name,
        int capacity,
        TopicBufferFullMode fullMode,
        Channel<TEnvelope> incomingMessages)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _channelOptions = new BoundedChannelOptions(capacity)
        {
            FullMode = (BoundedChannelFullMode)(int)fullMode
        };
        _incoming = incomingMessages;
        BeginProcessing();
    }

    /// <summary>
    /// The name of this topic.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Allows to write to the incoming message channel.
    /// </summary>
    protected ChannelWriter<TEnvelope> Incoming => _incoming;

    public async ValueTask<ISourceStream<TMessage>?> TrySubscribeAsync(
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

                var channel = CreateBounded<TEnvelope>(_channelOptions);
                var stream = new DefaultSourceStream<TEnvelope, TMessage>(channel);
                _outgoing.Add(channel);

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

    private void BeginProcessing()
        => Task.Factory.StartNew(
            async () => await ProcessMessagesAsync().ConfigureAwait(false),
            TaskCreationOptions.LongRunning);

    private async Task ProcessMessagesAsync()
    {
        using var subscription = await ConnectAsync(_incoming.Writer).ConfigureAwait(false);
        var closedChannels = new List<Channel<TEnvelope>>();
        var postponedMessages = new List<PostponedMessage>();

        while (!_incoming.Reader.Completion.IsCompleted)
        {
            if (await _incoming.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                DispatchMessages(closedChannels, postponedMessages);

                if (postponedMessages.Count > 0)
                {
                    for (var i = 0; i < postponedMessages.Count; i++)
                    {
                        var postponedMessage = postponedMessages[i];
                        var channel = postponedMessage.Channel;
                        var message = postponedMessage.Message;

                        try
                        {
                            await channel.Writer.WriteAsync(message).ConfigureAwait(false);
                        }
                        catch (ChannelClosedException)
                        {
                            // the channel might have been closed in the meantime.
                            // we will skip over this error and the channel will be collected
                            // on the next iteration.
                        }
                    }
                    postponedMessages.Clear();
                }

                await _semaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    if (closedChannels.Count > 0)
                    {
                        _outgoing.RemoveAll(c => closedChannels.Contains(c));
                        closedChannels.Clear();
                    }

                    // raises unsubscribed event only once all outgoing channels
                    // (subscriptions) are removed
                    if (_outgoing.Count == 0)
                    {
                        _closed = true;
                        RaiseUnsubscribedEvent();
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }
    }

    /// <summary>
    /// Override this method to connect to an external pub/sub provider.
    /// </summary>
    /// <param name="incoming">
    /// Write incoming messages to this channel writer.
    /// </param>
    /// <returns>
    /// Returns a session to dispose the subscription session.
    /// </returns>
    protected virtual ValueTask<IDisposable> ConnectAsync(ChannelWriter<TEnvelope> incoming)
        => new(DefaultSession.Instance);

#if NETSTANDARD2_0
    private void DispatchMessages(
        List<Channel<TEnvelope>> closedChannels,
        List<PostponedMessage> postponedMessages)
    {
        var batchSize = 4;
        var dispatched = 0;

        while (_incoming.Reader.TryRead(out var message))
        {
            // we are not locking at this point since the only thing happening to this list
            // is that new subscribers are added. This thread we are in is handling removals,
            // so we just grab the internal array and iterate over the window we have.
            var subscriberCount = _outgoing.Count;

            for (var i = 0; i < subscriberCount; i++)
            {
                var channel = _outgoing[i];

                if (!channel.Writer.TryWrite(message))
                {
                    if (channel.Reader.Completion.IsCompleted)
                    {
                        // if we detect channels that unsubscribed we will take a break and
                        // reorganize the subscriber list.
                        closedChannels.Add(channel);
                        batchSize = 0;
                    }
                    else
                    {
                        // if we cannot write because of backpressure we will postpone the message
                        // and take a break from processing further.
                        postponedMessages.Add(new PostponedMessage(message, channel));
                        batchSize = 0;
                    }
                }
            }

            // we try to avoid full message processing cycles and keep on dispatching messages,
            // but we will interrupt every 4 messages and allow for new subscribers
            // to join in.
            if (++dispatched >= batchSize)
            {
                break;
            }
        }
    }
#else
    private void DispatchMessages(
        List<Channel<TEnvelope>> closedChannels,
        List<PostponedMessage> postponedMessages)
    {
        var batchSize = 4;
        var dispatched = 0;

        while (_incoming.Reader.TryRead(out var message))
        {
            // we are not locking at this point since the only thing happening to this list
            // is that new subscribers are added. This thread we are in is handling removals,
            // so we just grab the internal array and iterate over the window we have.
            var subscriberCount = _outgoing.Count;
            var outgoingSpan = AsSpan(_outgoing);

            for (var i = 0; i < subscriberCount; i++)
            {
                var channel = outgoingSpan[i];

                if (!channel.Writer.TryWrite(message))
                {
                    if (channel.Reader.Completion.IsCompleted)
                    {
                        // if we detect channels that unsubscribed we will take a break and
                        // reorganize the subscriber list.
                        closedChannels.Add(channel);
                        batchSize = 0;
                    }
                    else
                    {
                        // if we cannot write because of backpressure we will postpone the message
                        // and take a break from processing further.
                        postponedMessages.Add(new PostponedMessage(message, channel));
                        batchSize = 0;
                    }
                }
            }

            // we try to avoid full message processing cycles and keep on dispatching messages,
            // but we will interrupt every 4 messages and allow for new subscribers
            // to join in.
            if (++dispatched >= batchSize)
            {
                break;
            }
        }
    }
#endif

    private sealed class PostponedMessage
    {
        public PostponedMessage(
            TEnvelope message,
            Channel<TEnvelope> channel)
        {
            Message = message;
            Channel = channel;
        }

        public TEnvelope Message { get; }

        public Channel<TEnvelope> Channel { get; }
    }

    private sealed class DefaultSession : IDisposable
    {
        private DefaultSession() { }
        public void Dispose() { }

        public static readonly DefaultSession Instance = new();
    }

    private void RaiseUnsubscribedEvent()
        => Unsubscribed?.Invoke(this, EventArgs.Empty);

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
                _incoming.Writer.TryComplete();
                _semaphore.Dispose();
                _outgoing.Clear();
            }

            _closed = true;
            _disposed = true;
        }
    }
}
