using System.Threading.Channels;
using AlterNats;
using HotChocolate.Subscriptions.InMemory;
using static System.Runtime.InteropServices.CollectionsMarshal;
using static System.Threading.Channels.Channel;

namespace HotChocolate.Subscriptions.Nats;

internal sealed class EventTopic<TMessage> : IEventTopic
{
    private static readonly EventMessageEnvelope<TMessage> _completed = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Channel<EventMessageEnvelope<TMessage>> _incoming;
    private readonly List<Channel<EventMessageEnvelope<TMessage>>> _outgoing = new();
    private readonly NatsConnection _connection;
    private readonly BoundedChannelOptions _channelOptions;
    private bool _closed;
    private bool _disposed;

    public event EventHandler<EventArgs>? Unsubscribed;

    public EventTopic(
        string topic,
        NatsConnection connection,
        int capacity,
        NatsTopicBufferFullMode fullMode)
    {
        Topic = topic;
        _connection = connection;
        _channelOptions = new BoundedChannelOptions(capacity)
        {
            FullMode = (BoundedChannelFullMode)(int)fullMode
        };
        _incoming = CreateBounded<EventMessageEnvelope<TMessage>>(_channelOptions);
        BeginProcessing();
    }

    public string Topic { get; }

    public async ValueTask<NatsSourceStream<TMessage>?> TrySubscribeAsync(
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

                var channel = CreateBounded<EventMessageEnvelope<TMessage>>(_channelOptions);
                var stream = new NatsSourceStream<TMessage>(channel);
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
        using var subscription = await ConnectAsync().ConfigureAwait(false);
        var closedChannels = new List<Channel<EventMessageEnvelope<TMessage>>>();
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
                        await channel.Writer.WriteAsync(message).ConfigureAwait(false);
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

    private ValueTask<IDisposable> ConnectAsync()
        => _connection.SubscribeAsync(
            Topic,
            (EventMessageEnvelope<TMessage> message)
                => EnqueueMessageAsync(message));

    private async Task EnqueueMessageAsync(EventMessageEnvelope<TMessage> message)
        => await _incoming.Writer.WriteAsync(message).ConfigureAwait(false);

    private void DispatchMessages(
        List<Channel<EventMessageEnvelope<TMessage>>> closedChannels,
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

    private sealed class PostponedMessage
    {
        public PostponedMessage(
            EventMessageEnvelope<TMessage> message,
            Channel<EventMessageEnvelope<TMessage>> channel)
        {
            Message = message;
            Channel = channel;
        }

        public EventMessageEnvelope<TMessage> Message { get; }

        public Channel<EventMessageEnvelope<TMessage>> Channel { get; }
    }

    private void RaiseUnsubscribedEvent()
        => Unsubscribed?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        if (!_disposed)
        {
            _incoming.Writer.TryComplete();
            _semaphore.Dispose();
            _outgoing.Clear();
            _closed = true;
            _disposed = true;
        }
    }
}
