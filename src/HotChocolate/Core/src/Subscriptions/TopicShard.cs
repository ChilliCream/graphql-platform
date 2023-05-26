using System.Threading.Channels;
using HotChocolate.Execution;
using HotChocolate.Subscriptions.Diagnostics;
using static System.Runtime.InteropServices.CollectionsMarshal;

namespace HotChocolate.Subscriptions;

internal sealed class TopicShard<TMessage>
{
    private readonly TaskCompletionSource<bool> _completion = new();
    private readonly Channel<TMessage> _incoming;
    private readonly List<DefaultSourceStream<TMessage>> _newSubscribers = new();
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

        _incoming = Channel.CreateBounded<TMessage>(_channelOptions);

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

        lock (_newSubscribers)
        {
            _newSubscribers.Add(stream);
        }

        return stream;
    }

    public void Unsubscribe(DefaultSourceStream<TMessage> channel)
    {
        lock (_subscribers)
        {
            _diagnosticEvents.Unsubscribe(_topicName, _topicShard, 1);
            _subscribers.Remove(channel);
            Unsubscribed?.Invoke(1);
        }
    }

    public void Complete() => _completion.TrySetResult(true);

    private void BeginProcessing()
        => Task.Factory.StartNew(
            async () => await ProcessMessagesAsync().ConfigureAwait(false));

    private async ValueTask ProcessMessagesAsync()
    {
        var closedChannels = new List<DefaultSourceStream<TMessage>>();
        var postponedMessages = new List<PostponedMessage>();
        var reader = _incoming.Reader;

        try
        {
            while (!reader.Completion.IsCompleted)
            {
                if (reader.TryPeek(out _))
                { DispatchMessages(closedChannels, postponedMessages);
                    await DispatchDelayedMessagesAsync(postponedMessages).ConfigureAwait(false);
                    UnsubscribeClosedChannels(closedChannels);
                }
                else
                {
                    if (_completion.Task.IsCompleted)
                    {
                        break;
                    }

                    await Task.WhenAny(_completion.Task, WaitForMessages()).ConfigureAwait(false);

                    if (_completion.Task.IsCompleted && !reader.TryPeek(out _))
                    {
                        break;
                    }
                }
            }
        }
        finally
        {
            _incoming.Writer.TryComplete();

            lock (_subscribers)
            {
                foreach (var subscriber in _subscribers)
                {
                    subscriber.Complete();
                }
            }
        }

        async Task WaitForMessages() => await reader.WaitToReadAsync();
    }

    private void DispatchMessages(
        List<DefaultSourceStream<TMessage>> closedChannels,
        List<PostponedMessage> postponedMessages)
    {
        var batchSize = 4;
        var dispatched = 0;

        lock (_subscribers)
        {
            if (_newSubscribers.Count > 0)
            {
                lock (_newSubscribers)
                {
                    _subscribers.AddRange(_newSubscribers);
                    _newSubscribers.Clear();
                }
            }

            while (_incoming.Reader.TryRead(out var message))
            {
                // we are not locking at this point since the only thing happening to this list
                // is that new subscribers are added. This thread we are in is handling removals,
                // so we just grab the internal array and iterate over the window we have.
                var outgoingSpan = AsSpan(_subscribers);
                var subscriberCount = outgoingSpan.Length;

                // if this shard is empty we will just return.
                if (subscriberCount == 0)
                {
                    return;
                }

                for (var i = 0; i < subscriberCount; i++)
                {
                    var sourceStream = outgoingSpan[i];

                    if (sourceStream.TryWrite(message))
                    {
                        continue;
                    }

                    // if we could not write to the channel we will check if the channel is closed.
                    if (sourceStream.IsCompleted)
                    {
                        // if we detect channels that unsubscribed we will take a break and
                        // reorganize the subscriber list.
                        closedChannels.Add(sourceStream);
                        batchSize = 0;
                    }
                    else
                    {
                        // if we cannot write because of back pressure we will postpone
                        // the message and take a break from processing further.
                        postponedMessages.Add(new PostponedMessage(message, sourceStream.Outgoing));
                        batchSize = 0;
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
    }

    private async ValueTask DispatchDelayedMessagesAsync(List<PostponedMessage> postponedMessages)
    {
        if (postponedMessages.Count > 0)
        {
            _diagnosticEvents.DelayedDispatch(
                _topicName,
                _topicShard,
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

    private void UnsubscribeClosedChannels(
        List<DefaultSourceStream<TMessage>> closedChannels)
    {
        if (closedChannels.Count > 0)
        {
            _diagnosticEvents.Unsubscribe(_topicName, _topicShard, closedChannels.Count);

            lock (_subscribers)
            {
                foreach (var channel in closedChannels)
                {
                    _subscribers.Remove(channel);
                }
            }

            Unsubscribed?.Invoke(closedChannels.Count);
            closedChannels.Clear();
        }
    }

    private sealed class PostponedMessage
    {
        public PostponedMessage(
            TMessage message,
            Channel<TMessage> channel)
        {
            Message = message;
            Channel = channel;
        }

        public TMessage Message { get; }

        public Channel<TMessage> Channel { get; }
    }
}
