using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnUnion.SubgraphC;

public sealed class MovieType : ObjectType<Movie>
{
    protected override void Configure(IObjectTypeDescriptor<Movie> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(m => m.Id).Type<NonNullType<IdType>>();
        descriptor.Field(m => m.Title).Shareable().Type<StringType>();
    }

    private static Movie? ResolveById(string id)
        => SubgraphCData.MoviesById.TryGetValue(id, out var movie) ? movie : null;
}
