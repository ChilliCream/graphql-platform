using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using AlterNats;
using HotChocolate.Execution;

namespace HotChocolate.Subscriptions.Nats;

// ReSharper disable once ClassNeverInstantiated.Global
public class NatsPubSub : ITopicEventReceiver, ITopicEventSender
{
    internal const string Completed = "{completed}";
    private readonly NatsConnection _connection;
    private readonly string _prefix;
    private readonly ConcurrentDictionary<string, string> _subjects = new();

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
        var subscription = await _connection.SubscribeAsync(subject, async (TMessage message) =>
        {
            if (message!.ToString() == Completed)
            {
                channel.Writer.Complete();
            }
            else
            {
                await channel.Writer.WriteAsync((TMessage)message, cancellationToken).ConfigureAwait(false);
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

        await _connection.PublishAsync(subject, message).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask CompleteAsync<TTopic>(TTopic topic) where TTopic : notnull
    {
        Debug.Assert(topic != null);
        string subject = GetSubject(topic);

        await _connection.PublishAsync(subject, Completed).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetSubject<TTopic>(TTopic topic) where TTopic : notnull
    {
        return _subjects.GetOrAdd(topic.ToString()!, static (t,p) => string.Concat(p, ".", t), _prefix);
    }
}
