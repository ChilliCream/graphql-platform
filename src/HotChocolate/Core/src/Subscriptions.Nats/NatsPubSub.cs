using System.Collections.Concurrent;
using System.Data.HashFunction.SpookyHash;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using HotChocolate.Execution;
using AlterNats;
using MessagePack;

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
    public async ValueTask<ISourceStream<TMessage>> SubscribeAsync<TTopic, TMessage>(TTopic topic,
        CancellationToken cancellationToken = default)
        where TTopic : notnull
    {
        Debug.Assert(topic != null);
        var subject = GetSubject(topic);

        var channel = Channel.CreateUnbounded<TMessage>();
        var subscription = await _connection.SubscribeAsync(
            subject,
            async (NatsMessageEnvelope<TMessage> message) =>
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
        var subject = GetSubject(topic);

        await _connection.PublishAsync(subject, new NatsMessageEnvelope<TMessage>(message)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask CompleteAsync<TTopic>(TTopic topic) where TTopic : notnull
    {
        Debug.Assert(topic != null);
        var subject = GetSubject(topic);

        await _connection.PublishAsync(subject, NatsMessageEnvelope<object>.Completed).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetSubject<TTopic>(TTopic topic) where TTopic : notnull
        => _subjects.GetOrAdd(topic, static (topic, tuple) =>
        {
            var (prefix, hasher) = tuple;

            var subject = Convert.ToHexString(
                hasher.ComputeHash(
                    MessagePackSerializer.Serialize(topic)).Hash);

            return string.Concat(prefix, ".", subject);

        }, (_prefix, _hasher));
}
