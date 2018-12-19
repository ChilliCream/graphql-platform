using System.Threading.Tasks;

namespace HotChocolate.Subscriptions
{
    /// <summary>
    /// The event registry manages the subscriptions to events.
    /// </summary>
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
        Task<IEventStream> SubscribeAsync(IEventDescription eventDescription);
    }
}
