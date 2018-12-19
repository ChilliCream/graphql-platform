using HotChocolate.Types;

namespace HotChocolate.Integration.StarWarsCodeFirst
{
    public class MutationType
        : ObjectType<Mutation>
    {
        protected override void Configure(IObjectTypeDescriptor<Mutation> descriptor)
        {
            descriptor.Field(t => t.CreateReview(default, default, default))
                .Type<NonNullType<ReviewType>>()
                .Argument("review", a => a.Type<NonNullType<ReviewInputType>>());
        }
    }
}
