namespace HotChocolate.Fusion.Suites.RequiresRequires.A;

/// <summary>
/// The <c>Product</c> entity in subgraph <c>a</c>.
/// Owns <c>price</c> (marked <c>@inaccessible</c>).
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public double Price { get; init; }
}
