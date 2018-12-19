using HotChocolate.Subscriptions;

namespace HotChocolate.Integration.StarWarsCodeFirst
{
    public class Subscription
    {
        public Review OnCreateReview(Episode episode, IEventMessage message)
        {
            return (Review)message.Payload;
        }
    }
}
