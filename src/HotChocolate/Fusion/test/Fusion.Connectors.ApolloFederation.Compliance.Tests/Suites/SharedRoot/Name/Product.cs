namespace HotChocolate.Fusion.Suites.SharedRoot.Name;

/// <summary>
/// The <c>Product</c> projection owned by the <c>name</c> subgraph.
/// Carries the shared <c>id</c> and the <c>name</c> link only.
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public Name Name { get; init; } = default!;
}
