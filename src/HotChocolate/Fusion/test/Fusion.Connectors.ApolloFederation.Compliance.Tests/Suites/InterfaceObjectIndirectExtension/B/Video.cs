namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.B;

/// <summary>
/// The <c>Video</c> entity as projected by the <c>b</c> subgraph
/// (<c>extend type Video @key(fields: "id")</c>, contributing
/// <c>authorName</c>).
/// </summary>
public sealed class Video
{
    public string Id { get; init; } = default!;
}
