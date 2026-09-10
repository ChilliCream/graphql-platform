namespace HotChocolate.Fusion.Suites.RequiresRequires.B;

/// <summary>
/// The <c>Product</c> entity in subgraph <c>b</c>.
/// Owns <c>hasDiscount</c>.
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public bool HasDiscount { get; init; }
}
