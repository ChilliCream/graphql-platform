using HotChocolate.Language;
using HotChocolate.Subscriptions;
using StarWars.Models;

namespace StarWars
{
    public class OnReviewMessage
        : EventMessage
    {
        public OnReviewMessage(Episode episode, Review review)
            : base(CreateEventDescription(episode), review)
        {
        }

        private static EventDescription CreateEventDescription(Episode episode)
        {
            return new EventDescription("onReview",
                new ArgumentNode("episode",
                    new EnumValueNode(
                        episode.ToString().ToUpperInvariant())));
        }
    }
}
