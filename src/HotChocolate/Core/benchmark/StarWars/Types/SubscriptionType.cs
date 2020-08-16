using HotChocolate.StarWars.Models;
using HotChocolate.Types;

namespace HotChocolate.StarWars.Types
{
    public class SubscriptionType
        : ObjectType<Subscription>
    {
        protected override void Configure(IObjectTypeDescriptor<Subscription> descriptor)
        {
            descriptor
                .Field(t => t.OnReview(default, default, default))
                .SubscribeToTopic<Episode, Review>("episode")
                .Type<NonNullType<ReviewType>>()
                .Argument("episode", arg => arg.Type<NonNullType<EpisodeType>>());
        }
    }
}
