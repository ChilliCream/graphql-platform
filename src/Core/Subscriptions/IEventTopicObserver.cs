using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Subscriptions
{
    /// <summary>
    /// The <see cref="IEventTopicObserver" /> creates subscription to
    /// a specific event topic and returns an <see cref="IEventStream{TMessage}" />
    /// which represents a stream of event message for the specified topic.
    /// </summary>
    public interface IEventTopicObserver
    {
        /// <summary>
        /// Subscribes to the specified event <paramref name="topic" />.
        /// </summary>
        /// <param name="topic">
        /// The topic to which the event message belongs to.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// Returns a <see cref="IEventStream{TMessage}" />
        /// for the given event <paramref name="topic" />.
        /// </returns>
        ValueTask<IEventStream<TMessage>> SubscribeAsync<TTopic, TMessage>(
            TTopic topic,
            CancellationToken cancellationToken = default)
            where TTopic : notnull;
    }
}
