using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionIntersection.A;

public sealed class SongType : ObjectType<Song>
{
    protected override void Configure(IObjectTypeDescriptor<Song> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(s => s.Id).Type<NonNullType<IdType>>();
        descriptor.Field(s => s.Title).Shareable().Type<NonNullType<StringType>>();
        descriptor.Field(s => s.ATitle).Type<NonNullType<StringType>>();
    }

    private static Song? ResolveById(string id)
        => string.Equals(id, AData.SongData.Id, StringComparison.Ordinal)
            ? AData.SongData
            : null;
}
