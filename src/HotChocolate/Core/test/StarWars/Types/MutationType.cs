using HotChocolate.Types;

namespace HotChocolate.StarWars.Types;

public class MutationType
    : ObjectType<Mutation>
{
    protected override void Configure(IObjectTypeDescriptor<Mutation> descriptor)
    {
        descriptor
            .Field(t => t.CreateReview(default, null!, null!, null!))
            .Type<NonNullType<ReviewType>>()
            .Argument("episode", a => a.Type<NonNullType<EpisodeType>>())
            .Argument("review", a => a.Type<NonNullType<ReviewInputType>>());
    }
}
