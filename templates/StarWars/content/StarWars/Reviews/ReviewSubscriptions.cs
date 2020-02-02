using HotChocolate;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using StarWars.Characters;
using StarWars.Repositories;

namespace StarWars.Reviews
{
    [ExtendObjectType(Name = "Subscription")]
    public class ReviewSubscriptions
    {
        public Review OnReview(
            Episode episode, 
            IEventMessage message, 
            [Service]IReviewRepository _repository)
        {
            return (Review)message.Payload;
        }
    }
}
