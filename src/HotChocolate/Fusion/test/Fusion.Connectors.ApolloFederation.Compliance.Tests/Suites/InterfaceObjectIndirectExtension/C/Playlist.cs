namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.C;

/// <summary>
/// The <c>Playlist</c> entity in the <c>c</c> subgraph
/// (<c>type Playlist @key(fields: "id")</c>).
/// </summary>
public sealed class Playlist
{
    public string Id { get; init; } = default!;

    public string? Name { get; init; }

    public Media? Media { get; init; }
}
