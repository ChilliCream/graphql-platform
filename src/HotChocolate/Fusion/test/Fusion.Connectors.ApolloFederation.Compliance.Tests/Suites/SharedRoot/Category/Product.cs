namespace HotChocolate.Fusion.Suites.SharedRoot.Category;

/// <summary>
/// The <c>Product</c> projection owned by the <c>category</c> subgraph.
/// Carries the shared <c>id</c> and the <c>category</c> link only.
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public Category Category { get; init; } = default!;
}
