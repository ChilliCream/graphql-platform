using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Subscriptions
{
    /// <summary>
    /// The event registry manages the subscriptions to events.
    /// </summary>
    [Obsolete("Use HotChocolate.Subscriptions.IEventTopicObserver.")]
    public interface IEventRegistry
    {
        /// <summary>
        /// Subscribes to an event specified by
        /// <paramref name="eventDescription"/>.
        /// </summary>
        /// <returns>
        /// Returns an event stream which yields the event
        /// messages of the subscribed event.
        /// </returns>
        /// <param name="eventDescription">
        /// The event description.
        /// </param>
        /// <param name="eventDescription">
        /// The cancellation token.
        /// </param>
        ValueTask<IEventStream> SubscribeAsync(
            IEventDescription eventDescription,
            CancellationToken cancellationToken = default);
    }
}
