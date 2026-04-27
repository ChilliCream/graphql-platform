using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnUnion.SubgraphB;

public sealed class MovieType : ObjectType<Movie>
{
    protected override void Configure(IObjectTypeDescriptor<Movie> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(m => m.Id).Type<NonNullType<IdType>>();
    }

    private static Movie? ResolveById(string id) => new Movie { Id = id };
}
