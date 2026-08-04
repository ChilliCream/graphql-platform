namespace HotChocolate.Fusion.Suites.SharedRoot.Price;

/// <summary>
/// The <c>Product</c> projection owned by the <c>price</c> subgraph.
/// Carries the shared <c>id</c> and the <c>price</c> link only.
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public Price Price { get; init; } = default!;
}
