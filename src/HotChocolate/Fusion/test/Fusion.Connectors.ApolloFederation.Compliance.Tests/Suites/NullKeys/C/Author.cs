namespace HotChocolate.Fusion.Suites.NullKeys.C;

/// <summary>
/// The <c>Author</c> value type owned by the <c>c</c> subgraph.
/// </summary>
public sealed class Author
{
    public string Id { get; init; } = default!;

    public string? Name { get; init; }
}
