namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.B;

/// <summary>
/// The <c>Author</c> entity in the <c>b</c> subgraph
/// (<c>type Author @key(fields: "id")</c>).
/// </summary>
public sealed class Author
{
    public string Id { get; init; } = default!;

    public string? Name { get; init; }
}
