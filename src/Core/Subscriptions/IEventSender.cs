using System.Threading.Tasks;

namespace HotChocolate.Subscriptions
{
    /// <summary>
    /// The event sender sends event messages to the pub/sub-system.
    /// Typically a mutation would use the event sender to raise events
    /// after some changes were commited to the backend system.
    ///
    /// Moreover, the <see cref="IEventSender"/> could also be used from outside
    /// the GraphQL schema process to raise events that than will trigger
    /// subscriptions to yield new results to their subscribers.
    /// </summary>
    public interface IEventSender
    {
        /// <summary>
        /// Sends an event message to the pub/sub-system.
        /// </summary>
        /// <param name="message">
        /// The event message.
        /// </param>
        Task SendAsync(IEventMessage message);
    }
}
