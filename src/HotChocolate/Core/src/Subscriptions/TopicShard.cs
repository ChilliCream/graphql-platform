using System.Threading.Channels;
using HotChocolate.Execution;
using HotChocolate.Subscriptions.Diagnostics;
using static System.Runtime.InteropServices.CollectionsMarshal;

namespace HotChocolate.Subscriptions;

internal sealed class TopicShard<TMessage>
{
    private readonly object _sync = new();
    private readonly Channel<MessageEnvelope<TMessage>> _incoming;
    private readonly List<Channel<MessageEnvelope<TMessage>>> _outgoing = new();
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

        _incoming = Channel.CreateBounded<MessageEnvelope<TMessage>>(_channelOptions);

        BeginProcessing();
    }

    public event Action<int>? Unsubscribed;

    /// <summary>
    /// Gets the count of current subscribers.
    /// </summary>
    public int Subscribers => _outgoing.Count;

    public ChannelWriter<MessageEnvelope<TMessage>> Writer => _incoming.Writer;

    public ISourceStream<TMessage> Subscribe()
    {
        var channel = Channel.CreateBounded<MessageEnvelope<TMessage>>(_channelOptions);
        var stream = new DefaultSourceStream<TMessage>(_incoming.Writer, channel);
        var outgoing = _outgoing;

        lock (_sync)
        {
            outgoing.Add(channel);
        }

        return stream;
    }

    private void BeginProcessing()
        => Task.Factory.StartNew(
            async () => await ProcessMessagesAsync().ConfigureAwait(false));

    private async ValueTask ProcessMessagesAsync()
    {
        var closedChannels = new List<Channel<MessageEnvelope<TMessage>>>();
        var postponedMessages = new List<PostponedMessage>();
        var reader = _incoming.Reader;

        while (!reader.Completion.IsCompleted)
        {
            if (await reader.WaitToReadAsync().ConfigureAwait(false))
            {
                DispatchMessages(closedChannels, postponedMessages);

                await DispatchDelayedMessagesAsync(postponedMessages).ConfigureAwait(false);

                UnsubscribeClosedChannels(closedChannels);
            }
        }
    }

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

            // if this shard is empty we will just return.
            if (subscriberCount == 0)
            {
                return;
            }

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
        List<Channel<MessageEnvelope<TMessage>>> closedChannels)
    {
        if (closedChannels.Count > 0)
        {
            _diagnosticEvents.Unsubscribe(_topicName, _topicShard, closedChannels.Count);

            lock (_sync)
            {
                _outgoing.RemoveAll(c => closedChannels.Contains(c));
            }

            Unsubscribed?.Invoke(closedChannels.Count);
            closedChannels.Clear();
        }
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
}
