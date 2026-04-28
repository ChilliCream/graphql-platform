namespace HotChocolate.Fusion.Suites.RequiresWithArgument.D;

/// <summary>
/// The <c>Author</c> type in the <c>d</c> subgraph. Not an entity,
/// resolved locally from the post's required comments data.
/// </summary>
public sealed class Author
{
    public string Id { get; init; } = default!;

    public string? Name { get; init; }
}
