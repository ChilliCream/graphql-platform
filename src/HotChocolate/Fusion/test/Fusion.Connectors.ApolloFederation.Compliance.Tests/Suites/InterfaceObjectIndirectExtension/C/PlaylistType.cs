using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.C;

/// <summary>
/// Apollo Federation descriptor for the <c>Playlist</c> entity in the
/// <c>c</c> subgraph, keyed by <c>id</c>, with a <c>media: Media</c> field.
/// </summary>
public sealed class PlaylistType : ObjectType<Playlist>
{
    protected override void Configure(IObjectTypeDescriptor<Playlist> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.Name).Type<StringType>();
        descriptor.Field(p => p.Media).Type<MediaType>();
    }

    private static Playlist ResolveById(string id) => CData.PlaylistById(id);
}
