using System.Collections.Concurrent;
using AlterNats;
using HotChocolate.Execution;
using static System.StringComparer;

namespace HotChocolate.Subscriptions.Nats;

internal sealed class NatsPubSub : ITopicEventReceiver, ITopicEventSender, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, IDisposable> _topics = new(Ordinal);
    private readonly TopicFormatter _formatter;
    private readonly SubscriptionOptions _options;
    private readonly NatsConnection _connection;
    private readonly IMessageSerializer _serializer;
    private readonly string _completed;
    private readonly CancellationToken _abortBackgroundWork;
    private readonly TimeSpan _pingTimeout = TimeSpan.FromSeconds(30);
    private bool _disposed;

    public NatsPubSub(
        NatsConnection connection,
        IMessageSerializer serializer,
        SubscriptionOptions options)
    {
        _connection = connection;
        _serializer = serializer;
        _completed = serializer.CompleteMessage;
        _options = options;
        _formatter = new TopicFormatter(options.TopicPrefix);
        _abortBackgroundWork = _cts.Token;

        // we start ping messages to the server to keep the connection alive.
        BeginPingPong();
    }

    public async ValueTask<ISourceStream<TMessage>> SubscribeAsync<TMessage>(
        string topic,
        int? bufferCapacity = null,
        TopicBufferFullMode? bufferFullMode = null,
        CancellationToken cancellationToken = default)
    {
        if (topic is null)
        {
            throw new ArgumentNullException(nameof(topic));
        }

        var formattedTopic = _formatter.Format(topic);
        ISourceStream<TMessage>? sourceStream = null;

        while (sourceStream is null)
        {
            var eventTopic = _topics.GetOrAdd(
                formattedTopic,
                t => CreateTopic<TMessage>(t, bufferCapacity, bufferFullMode));

            if (eventTopic is NatsTopic<TMessage> et)
            {
                sourceStream = await et.TrySubscribeAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // we found a topic with the same name but a different message type.
                // this is an invalid state and we will except.
                throw new InvalidMessageTypeException();
            }
        }

        return sourceStream;
    }

    public async ValueTask SendAsync<TMessage>(
        string topic,
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        if (topic is null)
        {
            throw new ArgumentNullException(nameof(topic));
        }

        var formattedTopic = _formatter.Format(topic);
        var envelope = new NatsMessageEnvelope<TMessage>(message, false);
        var serialized = _serializer.Serialize(envelope);
        await _connection.PublishAsync(formattedTopic, serialized).ConfigureAwait(false);
    }

    public async ValueTask CompleteAsync(string topic)
    {
        if (topic is null)
        {
            throw new ArgumentNullException(nameof(topic));
        }

        var formattedTopic = _formatter.Format(topic);
        await _connection.PublishAsync(formattedTopic, _completed).ConfigureAwait(false);
    }

    private NatsTopic<TMessage> CreateTopic<TMessage>(
        string topic,
        int? bufferCapacity,
        TopicBufferFullMode? bufferFullMode)
    {
        var eventTopic = new NatsTopic<TMessage>(
            topic,
            _connection,
            _serializer,
            bufferCapacity ?? _options.TopicBufferCapacity,
            bufferFullMode ?? _options.TopicBufferFullMode);

        eventTopic.Unsubscribed += (sender, __) =>
        {
            var s = (NatsTopic<TMessage>)sender!;
            _topics.TryRemove(s.Name, out _);
            s.Dispose();
        };

        return eventTopic;
    }

    private void BeginPingPong()
    {
        _connection.PostPing();

        Task.Factory.StartNew(
            async () =>
            {
                while (!_abortBackgroundWork.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(_pingTimeout, _abortBackgroundWork).ConfigureAwait(false);
                        _connection.PostPing();
                    }
                    catch (OperationCanceledException)
                    {
                        // we ignore this one and exit our work
                    }
                }
            },
            TaskCreationOptions.LongRunning);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cts.Cancel();
            _cts.Dispose();
            _disposed = true;
        }
    }
}
