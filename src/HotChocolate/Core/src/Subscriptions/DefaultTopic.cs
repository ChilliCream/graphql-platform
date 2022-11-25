using System.Threading.Channels;
using HotChocolate.Execution;
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
public abstract class DefaultTopic<TMessage> : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Channel<MessageEnvelope<TMessage>> _incoming;
    private readonly List<Channel<MessageEnvelope<TMessage>>> _outgoing = new();
    private readonly BoundedChannelOptions _channelOptions;
    private readonly ISubscriptionDiagnosticEvents _diagnosticEvents;
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
        _incoming = CreateBounded<MessageEnvelope<TMessage>>(_channelOptions);
        _diagnosticEvents = diagnosticEvents;
        diagnosticEvents.Created(Name);
    }

    protected DefaultTopic(
        string name,
        int capacity,
        TopicBufferFullMode fullMode,
        ISubscriptionDiagnosticEvents diagnosticEvents,
        Channel<MessageEnvelope<TMessage>> incomingMessages)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _channelOptions = new BoundedChannelOptions(capacity)
        {
            FullMode = (BoundedChannelFullMode)(int)fullMode
        };
        _incoming = incomingMessages;
        _diagnosticEvents = diagnosticEvents;
    }

    /// <summary>
    /// The name of this topic.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Allows to write to the incoming message channel.
    /// </summary>
    protected ChannelWriter<MessageEnvelope<TMessage>> Incoming => _incoming;

    /// <summary>
    /// Allows access to the diagnostic events.
    /// </summary>
    protected ISubscriptionDiagnosticEvents DiagnosticEvents => _diagnosticEvents;

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


                var outgoing = CreateBounded<MessageEnvelope<TMessage>>(_channelOptions);
                var stream = new DefaultSourceStream<TMessage>(_incoming.Writer, outgoing);
                _outgoing.Add(outgoing);
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

    internal async ValueTask ConnectAsync(CancellationToken ct = default)
    {
        try
        {
            var session = await OnConnectAsync(_incoming.Writer, ct).ConfigureAwait(false);
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
            session,
            TaskCreationOptions.LongRunning);

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
            await UnsubscribeAllChannels().ConfigureAwait(false);
            session.Dispose();
            DiagnosticEvents.Disconnected(Name);
        }
    }

    private async Task ProcessMessagesAsync()
    {
        var closedChannels = new List<Channel<MessageEnvelope<TMessage>>>();
        var postponedMessages = new List<PostponedMessage>();

        while (!_closed && !_incoming.Reader.Completion.IsCompleted)
        {
            _diagnosticEvents.WaitForMessages(Name);

            if (await _incoming.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                DispatchMessages(closedChannels, postponedMessages);

                await DispatchDelayedMessagesAsync(postponedMessages).ConfigureAwait(false);

                await UnsubscribeClosedChannels(closedChannels).ConfigureAwait(false);
            }
        }
    }

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
        ChannelWriter<MessageEnvelope<TMessage>> incoming,
        CancellationToken cancellationToken)
        => new(DefaultSession.Instance);

    private void DispatchMessages(
        List<Channel<MessageEnvelope<TMessage>>> closedChannels,
        List<PostponedMessage> postponedMessages)
    {
        var batchSize = 4;
        var dispatched = 0;

        while (_incoming.Reader.TryRead(out var message))
        {
            // we are not locking at this point since the only thing happening to this list
            // is that new subscribers are added. This thread we are in is handling removals,
            // so we just grab the internal array and iterate over the window we have.
            var outgoingSpan = AsSpan(_outgoing);
            var subscriberCount = outgoingSpan.Length;

            // if we get an unsubscribed message we will only collect closed channels.
            if (message.Kind is MessageKind.Unsubscribed)
            {
                for (var i = 0; i < subscriberCount; i++)
                {
                    var channel = outgoingSpan[i];

                    if (channel.Reader.Completion.IsCompleted)
                    {
                        // if we detect channels that unsubscribed we will take a break and
                        // reorganize the subscriber list.
                        closedChannels.Add(channel);
                        batchSize = 0;
                    }
                }
            }
            else
            {
                _diagnosticEvents.Dispatched(Name, message, subscriberCount);

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
                            // if we cannot write because of back pressure we will postpone
                            // the message and take a break from processing further.
                            postponedMessages.Add(new PostponedMessage(message, channel));
                            batchSize = 0;
                        }
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

    private async ValueTask DispatchDelayedMessagesAsync(List<PostponedMessage> postponedMessages)
    {
        if (postponedMessages.Count > 0)
        {
            _diagnosticEvents.Delayed(
                Name,
                postponedMessages[0].Message,
                postponedMessages.Count);

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
    }

    private async Task UnsubscribeClosedChannels(
        List<Channel<MessageEnvelope<TMessage>>> closedChannels)
    {
        if (_disposed)
        {
            return;
        }

        await _semaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            if (closedChannels.Count > 0)
            {
                _diagnosticEvents.Unsubscribe(Name, closedChannels.Count);
                _outgoing.RemoveAll(c => closedChannels.Contains(c));
                closedChannels.Clear();
            }

            // raises unsubscribed event only once all outgoing channels
            // (subscriptions) are removed
            if (_outgoing.Count == 0)
            {
                _closed = true;
            }
        }
        finally
        {
            _semaphore.Release();
        }

        if (_closed)
        {
            RaiseClosedEvent();
        }
    }

    private async Task UnsubscribeAllChannels()
    {
        if (_disposed)
        {
            return;
        }

        await _semaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            if (_outgoing.Count > 0)
            {
                _diagnosticEvents.Unsubscribe(Name, _outgoing.Count);

                foreach (var channel in _outgoing)
                {
                    channel.Writer.TryComplete();
                }

                _outgoing.Clear();
            }

            _closed = true;
        }
        finally
        {
            _semaphore.Release();
        }

        RaiseClosedEvent();
    }

    private sealed class PostponedMessage
    {
        public PostponedMessage(
            MessageEnvelope<TMessage> message,
            Channel<MessageEnvelope<TMessage>> channel)
        {
            Message = message;
            Channel = channel;
        }

        public MessageEnvelope<TMessage> Message { get; }

        public Channel<MessageEnvelope<TMessage>> Channel { get; }
    }

    private sealed class DefaultSession : IDisposable
    {
        private DefaultSession() { }
        public void Dispose() { }

        public static readonly DefaultSession Instance = new();
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
                _incoming.Writer.TryComplete();
                _semaphore.Dispose();
                _outgoing.Clear();
            }

            _closed = true;
            _disposed = true;
        }
    }
}
