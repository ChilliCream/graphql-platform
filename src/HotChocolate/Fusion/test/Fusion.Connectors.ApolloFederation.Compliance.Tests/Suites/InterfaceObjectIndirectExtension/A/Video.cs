namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.A;

/// <summary>
/// The <c>Video</c> entity in the <c>a</c> subgraph
/// (<c>type Video implements Media @key(fields: "id")</c>).
/// </summary>
public sealed class Video : IMedia
{
    public string Id { get; init; } = default!;

    public string? Title { get; init; }

    public int? Duration { get; init; }
}
