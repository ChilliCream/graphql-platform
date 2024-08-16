using HotChocolate.StarWars.Models;
using HotChocolate.Types;
using static HotChocolate.StarWars.Types.Subscriptions;

namespace HotChocolate.StarWars.Types;

public class SubscriptionType : ObjectType<Subscription>
{
    protected override void Configure(IObjectTypeDescriptor<Subscription> descriptor)
    {
        descriptor
            .Field(t => t.OnReview(default, default!, default!))
            .Argument("episode", arg => arg.Type<NonNullType<EpisodeType>>())
            .Type<NonNullType<ReviewType>>()
            .SubscribeToTopic<Review>(c => $"{OnReview}_{c.ArgumentValue<Episode>("episode")}");
    }
}
