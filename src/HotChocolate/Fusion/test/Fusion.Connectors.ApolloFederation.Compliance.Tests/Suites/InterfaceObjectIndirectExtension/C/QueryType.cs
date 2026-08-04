using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.C;

/// <summary>
/// Root <c>Query</c> for the <c>c</c> subgraph. Exposes <c>playlist: Playlist</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("playlist")
            .Type<PlaylistType>()
            .Resolve(_ => CData.DefaultPlaylist());
    }
}
