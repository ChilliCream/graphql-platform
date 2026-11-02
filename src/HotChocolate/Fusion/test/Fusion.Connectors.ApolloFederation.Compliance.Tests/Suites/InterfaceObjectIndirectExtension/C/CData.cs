namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.C;

/// <summary>
/// Seed data for the <c>c</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/interface-object-indirect-extension/playlist.subgraph.ts</c>.
/// </summary>
internal static class CData
{
    public static Playlist DefaultPlaylist() => PlaylistById("1");

    public static Playlist PlaylistById(string id)
        => new Playlist
        {
            Id = id,
            Name = $"name for {id}",
            Media = new Media { Id = id }
        };
}
