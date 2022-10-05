using System.Diagnostics;
using System.Threading.Channels;
using AlterNats;
using HotChocolate.Execution;

namespace HotChocolate.Subscriptions.Nats;

// ReSharper disable once ClassNeverInstantiated.Global
public class NatsPubSub : ITopicEventReceiver, ITopicEventSender
{
    internal const string Completed = "{completed}";
    private readonly NatsConnection _connection;

    public NatsPubSub(NatsConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc />
    public async ValueTask<ISourceStream<TMessage>> SubscribeAsync<TTopic, TMessage>(TTopic topic,
        CancellationToken cancellationToken = default)
        where TTopic : notnull
    {
        Debug.Assert(topic != null);

        var channel = Channel.CreateUnbounded<TMessage>();
        var subscription = await _connection.SubscribeAsync(topic.ToString()!, async (TMessage message) =>
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
        await _connection.PublishAsync(topic.ToString()!, message).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask CompleteAsync<TTopic>(TTopic topic) where TTopic : notnull
    {
        Debug.Assert(topic != null);
        await _connection.PublishAsync(topic.ToString()!, Completed).ConfigureAwait(false);
    }
}
