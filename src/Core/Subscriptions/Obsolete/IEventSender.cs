using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Subscriptions
{
    /// <summary>
    /// The event sender sends event messages to the pub/sub-system.
    /// Typically a mutation would use the event sender to raise events
    /// after some changes were committed to the backend system.
    ///
    /// Moreover, the <see cref="IEventSender"/> could also be used from outside
    /// the GraphQL schema process to raise events that than will trigger
    /// subscriptions to yield new results to their subscribers.
    /// </summary>
    [Obsolete("Use HotChocolate.Subscriptions.IEventDispatcher.")]
    public interface IEventSender
    {
        /// <summary>
        /// Sends an event message to the pub/sub-system.
        /// </summary>
        /// <param name="message">
        /// The event message.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        ValueTask SendAsync(
            IEventMessage message,
            CancellationToken cancellationToken = default);
    }
}
