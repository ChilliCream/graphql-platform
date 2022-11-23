using System.Collections.Concurrent;
using System.Data.HashFunction.SpookyHash;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using HotChocolate.Execution;
using AlterNats;
using HotChocolate.Subscriptions.InMemory;
using MessagePack;
using static System.StringComparer;

namespace HotChocolate.Subscriptions.Nats;

/// <summary>
/// Represents the NATS Pub/Sub connection.
/// </summary>
public sealed class NatsPubSub : ITopicEventReceiver, ITopicEventSender
{
    private readonly NatsConnection _connection;
    private readonly string _prefix;

    private readonly ConcurrentDictionary<object, string> _subjects = new();

    // http://burtleburtle.net/bob/hash/spooky.html
    private readonly ISpookyHash _hasher = SpookyHashV2Factory.Instance.Create();

    /// <summary>
    /// Initializes a new instance of <see cref="NatsPubSub"/>.
    /// </summary>
    /// <param name="connection">
    /// The underlying NATS connection.
    /// </param>
    /// <param name="prefix">
    /// The NATS prefix.
    /// </param>
    /// <exception cref="ArgumentException">
    /// The prefix cannot be null or empty.
    /// </exception>
    public NatsPubSub(NatsConnection connection, string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException(
                NatsResources.NatsPubSub_NatsPubSub_PrefixCannotBeNull,
                nameof(prefix));
        }

        _connection = connection;
        _prefix = prefix;
    }

    /// <inheritdoc />
    public async ValueTask<ISourceStream<TMessage>> SubscribeAsync<TTopic, TMessage>(
        TTopic topic,
        CancellationToken cancellationToken = default)
        where TTopic : notnull
    {
        Debug.Assert(topic != null);
        var subject = GetSubject(topic);

        var channel = Channel.CreateBounded<TMessage>(
            new BoundedChannelOptions(50) { FullMode = BoundedChannelFullMode.Wait })
        var subscription = await _connection.SubscribeAsync(
            subject,
            async (EventMessageEnvelope<TMessage> message) =>
            {
                // fixme
                if (message.MessageType == NatsMessageType.Completed)
                {
                    channel.Writer.Complete();
                }
                else
                {
                    await channel.Writer.WriteAsync(message.Body!, cancellationToken)
                        .ConfigureAwait(false);
                }
            }).ConfigureAwait(false);

        return await ValueTask.FromResult(new NatsSourceStream<TMessage>(channel, subscription))
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask SendAsync<TTopic, TMessage>(
        TTopic topic,
        TMessage message,
        CancellationToken cancellationToken = default) where TTopic : notnull
    {
        Debug.Assert(topic != null);
        var subject = GetSubject(topic);

        await _connection.PublishAsync(subject, new EventMessageEnvelope<TMessage>(message))
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask CompleteAsync<TTopic>(TTopic topic) where TTopic : notnull
    {
        Debug.Assert(topic != null);
        var subject = GetSubject(topic);

        await _connection.PublishAsync(subject, EventMessageEnvelope<object>.Completed)
            .ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetSubject<TTopic>(TTopic topic) where TTopic : notnull
        => _subjects.GetOrAdd(
            topic,
            static (topic, tuple) =>
            {
                var (prefix, hasher) = tuple;

                // We always serialize, even if TTopic is a string, because the string
                // may contain characters that are not allowed in a NATS subject.
                // NOTE: this can fail if the topic is not serializable.
                var subject = Convert.ToHexString(
                    hasher.ComputeHash(
                        MessagePackSerializer.Serialize(topic)).Hash);

                return string.Concat(prefix, ".", subject);
            },
            (_prefix, _hasher));
}

internal sealed class NatsPubSub2 : ITopicEventReceiver, ITopicEventSender
{
    private static readonly EventMessageEnvelope<object> _completed = new();
    private readonly ConcurrentDictionary<string, IEventTopic> _topics = new(Ordinal);
    private readonly NatsTopicFormatter _formatter;
    private readonly NatsPubSubOptions _options;
    private readonly NatsConnection _connection;

    public async ValueTask<ISourceStream<TMessage>> SubscribeAsync<TMessage>(
        string topic,
        CancellationToken cancellationToken = default)
    {
        if (topic is null)
        {
            throw new ArgumentNullException(nameof(topic));
        }

        var formattedTopic = _formatter.Format(topic);
        NatsSourceStream<TMessage>? sourceStream = null;

        while (sourceStream is null)
        {
            var eventTopic = _topics.GetOrAdd(formattedTopic, t => CreateTopic<TMessage>(t));

            if (eventTopic is EventTopic<TMessage> et)
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
        var envelope = new EventMessageEnvelope<TMessage>(message);
        await _connection.PublishAsync(formattedTopic, envelope).ConfigureAwait(false);
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

    private EventTopic<TMessage> CreateTopic<TMessage>(string topic)
    {
        var eventTopic = new EventTopic<TMessage>(
            topic,
            _connection,
            _options.TopicBufferCapacity,
            _options.TopicBufferFullMode);

        eventTopic.Unsubscribed += (sender, __) =>
        {
            var s = (EventTopic<TMessage>)sender!;
            _topics.TryRemove(s.Topic, out _);
            s.Dispose();
        };

        return eventTopic;
    }
}

internal sealed class NatsTopicFormatter
{
    // http://burtleburtle.net/bob/hash/spooky.html
    private readonly ISpookyHash _spookyHash = SpookyHashV2Factory.Instance.Create();
    private readonly string? _prefix;

    public NatsTopicFormatter(string? prefix)
    {
        _prefix = prefix;
    }

    public string Format(string topic)
    {
        if (_prefix is null)
        {
            Convert.ToHexString(_spookyHash.ComputeHash(topic).Hash);
        }
    }
}
