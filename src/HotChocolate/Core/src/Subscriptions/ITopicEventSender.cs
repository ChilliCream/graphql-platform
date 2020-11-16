using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Subscriptions
{
    /// <summary>
    /// The topic event sender sends event messages to the pub/sub-system.
    /// Typically a mutation would use the event dispatcher to raise events
    /// after some changes were committed to the backend system.
    /// </summary>
    public interface ITopicEventSender
    {
        /// <summary>
        /// Sends an event message to the pub/sub-system.
        /// </summary>
        /// <param name="topic">
        /// The topic to which the event message belongs to.
        /// </param>
        /// <param name="message">
        /// The event message.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        ValueTask SendAsync<TTopic, TMessage>(
            TTopic topic,
            TMessage message,
            CancellationToken cancellationToken = default)
            where TTopic : notnull;

        /// <summary>
        /// Completes a event topic which causes the
        /// <see cref="ISourceStream{TMessage}" /> to complete.
        /// </summary>
        /// <param name="topic">
        /// The topic to which the event message belongs to.
        /// </param>
        ValueTask CompleteAsync<TTopic>(TTopic topic)
            where TTopic : notnull;
    }
}
