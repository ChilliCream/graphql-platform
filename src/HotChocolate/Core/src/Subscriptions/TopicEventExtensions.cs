using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using static HotChocolate.Subscriptions.TopicFormatter;

namespace HotChocolate.Subscriptions;

/// <summary>
/// Provides extension methods to allow for the legacy signature of the subscription providers.
/// </summary>
public static class TopicEventExtensions
{
    /// <summary>
    /// Subscribes to the specified event <paramref name="topic" />.
    /// </summary>
    /// <param name="receiver">
    /// The event receiver.
    /// </param>
    /// <param name="topic">
    /// The topic to which the event message belongs to.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns a <see cref="ISourceStream{TMessage}" />
    /// for the given event <paramref name="topic" />.
    /// </returns>
    public static ValueTask<ISourceStream<TMessage>> SubscribeAsync<TTopic, TMessage>(
        this ITopicEventReceiver receiver,
        TTopic topic,
        CancellationToken cancellationToken = default)
        where TTopic : notnull
        => receiver.SubscribeAsync<TMessage>(Format(topic), cancellationToken);

    /// <summary>
    /// Sends an event message to the pub/sub-system.
    /// </summary>
    /// <param name="sender">
    /// The event sender.
    /// </param>
    /// <param name="topic">
    /// The topic to which the event message belongs to.
    /// </param>
    /// <param name="message">
    /// The event message.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    public static ValueTask SendAsync<TTopic, TMessage>(
        this ITopicEventSender sender,
        TTopic topic,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TTopic : notnull
        => sender.SendAsync(Format(topic), message, cancellationToken);

    /// <summary>
    /// Completes a event topic which causes the
    /// <see cref="ISourceStream{TMessage}" /> to complete.
    /// </summary>
    /// <param name="sender">
    /// The event sender.
    /// </param>
    /// <param name="topic">
    /// The topic to which the event message belongs to.
    /// </param>
    public static ValueTask CompleteAsync<TTopic>(
        this ITopicEventSender sender,
        TTopic topic)
        where TTopic : notnull
        => sender.CompleteAsync(Format(topic));
}
