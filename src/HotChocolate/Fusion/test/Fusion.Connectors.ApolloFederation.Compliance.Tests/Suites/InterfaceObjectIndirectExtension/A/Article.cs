namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.A;

/// <summary>
/// The <c>Article</c> entity in the <c>a</c> subgraph
/// (<c>type Article implements Media @key(fields: "id")</c>).
/// </summary>
public sealed class Article : IMedia
{
    public string Id { get; init; } = default!;

    public string? Title { get; init; }

    public int? WordCount { get; init; }
}
