using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionIntersection.B;

public sealed class MovieType : ObjectType<Movie>
{
    protected override void Configure(IObjectTypeDescriptor<Movie> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(m => m.Id).Type<NonNullType<IdType>>();
        descriptor.Field(m => m.Title).Shareable().Type<NonNullType<StringType>>();
        descriptor.Field(m => m.BTitle).Type<NonNullType<StringType>>();
    }

    private static Movie? ResolveById(string id)
        => string.Equals(id, BData.MovieData.Id, StringComparison.Ordinal)
            ? BData.MovieData
            : null;
}
