using System.Collections.Concurrent;
using System.Data.HashFunction.SpookyHash;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using AlterNats;
using HotChocolate.Execution;
using MessagePack;

namespace HotChocolate.Subscriptions.Nats;

public class NatsMessageEnvelope<TBody>
{
    public NatsMessageEnvelope(TBody? body, NatsMessageType messageType = NatsMessageType.Message)
    {
        if (messageType == NatsMessageType.Message && body == null)
        {
            throw new ArgumentNullException(nameof(body));
        }
        MessageType = messageType;
        Body = body;
    }

    public NatsMessageType MessageType { get; }
    public TBody? Body { get; }

    public static NatsMessageEnvelope<TBody> Completed { get; } = new(default, NatsMessageType.Completed);
}

public enum NatsMessageType
{
    Message,
    Completed
}

// ReSharper disable once ClassNeverInstantiated.Global
public class NatsPubSub : ITopicEventReceiver, ITopicEventSender
{
    private readonly NatsConnection _connection;
    private readonly string _prefix;
    private readonly ConcurrentDictionary<object, string> _subjects = new();
    // http://burtleburtle.net/bob/hash/spooky.html
    private readonly ISpookyHash _hasher = SpookyHashV2Factory.Instance.Create();

    public NatsPubSub(NatsConnection connection, string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException(@"Value cannot be null or whitespace.", nameof(prefix));

        _connection = connection;
        _prefix = prefix;

    }

    /// <inheritdoc />
    public async ValueTask<ISourceStream<TMessage>> SubscribeAsync<TTopic, TMessage>(TTopic topic,
        CancellationToken cancellationToken = default)
        where TTopic : notnull
    {
        Debug.Assert(topic != null);
        string subject = GetSubject(topic);

        var channel = Channel.CreateUnbounded<TMessage>();
        var subscription = await _connection.SubscribeAsync(subject, async (NatsMessageEnvelope<TMessage> message) =>
        {
            // fixme
            if (message.MessageType == NatsMessageType.Completed)
            {
                channel.Writer.Complete();
            }
            else
            {
                await channel.Writer.WriteAsync(message.Body!, cancellationToken).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);

        return await ValueTask.FromResult(new NatsEventStream<TMessage>(channel, subscription)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask SendAsync<TTopic, TMessage>(TTopic topic, TMessage message,
        CancellationToken cancellationToken = default) where TTopic : notnull
    {
        Debug.Assert(topic != null);
        string subject = GetSubject(topic);

        await _connection.PublishAsync(subject, new NatsMessageEnvelope<TMessage>(message)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask CompleteAsync<TTopic>(TTopic topic) where TTopic : notnull
    {
        Debug.Assert(topic != null);
        string subject = GetSubject(topic);

        await _connection.PublishAsync(subject, NatsMessageEnvelope<object>.Completed).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetSubject<TTopic>(TTopic topic) where TTopic : notnull
    {
        return _subjects.GetOrAdd(topic, static (topic, tuple) =>
        {
            var (prefix, hasher) = tuple;

            var subject = Convert.ToHexString(
                hasher.ComputeHash(
                    MessagePackSerializer.Serialize(topic)).Hash);

            return string.Concat(prefix, ".", subject);

        }, (_prefix, _hasher));
    }
}
