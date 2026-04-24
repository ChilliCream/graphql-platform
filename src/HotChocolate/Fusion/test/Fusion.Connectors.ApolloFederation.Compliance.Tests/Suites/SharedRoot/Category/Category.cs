namespace HotChocolate.Fusion.Suites.SharedRoot.Category;

/// <summary>
/// The <c>Category</c> value type owned by the <c>category</c> subgraph.
/// </summary>
public sealed class Category
{
    public string Id { get; init; } = default!;

    public string Name { get; init; } = default!;
}
