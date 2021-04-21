using HotChocolate;
using HotChocolate.Types;
using StarWars.Characters;

namespace StarWars.Reviews
{
    /// <summary>
    /// The subscriptions related to reviews.
    /// </summary>
    [ExtendObjectType(OperationTypeNames.Subscription)]
    public class ReviewSubscriptions
    {
        /// <summary>
        /// The OnReview event is invoked whenever a new review is being created.
        /// </summary>
        /// <param name="episode">
        /// The episode to which you want to subscribe to.
        /// </param>
        /// <param name="message">
        /// The event message.
        /// </param>
        /// <returns>
        /// The review that was created.
        /// </returns>
        [Subscribe]
        public Review OnReview(
            [Topic]Episode episode, 
            [EventMessage]Review message) => 
            message;
    }
}
