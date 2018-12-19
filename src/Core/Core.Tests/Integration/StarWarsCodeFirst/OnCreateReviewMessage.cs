using HotChocolate.Language;
using HotChocolate.Subscriptions;

namespace HotChocolate.Integration.StarWarsCodeFirst
{
    public class OnCreateReviewMessage
        : EventMessage
    {
        public OnCreateReviewMessage(Episode episode, Review review)
            : base(CreateDescription(episode), review)
        {
        }

        private static EventDescription CreateDescription(Episode episode) =>
            new EventDescription("onCreateReview",
                new ArgumentNode("episode", new EnumValueNode(episode)));
    }
}
