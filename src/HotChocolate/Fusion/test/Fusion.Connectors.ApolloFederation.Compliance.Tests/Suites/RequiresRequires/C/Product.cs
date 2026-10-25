namespace HotChocolate.Fusion.Suites.RequiresRequires.C;

/// <summary>
/// The <c>Product</c> entity in subgraph <c>c</c>.
/// <c>price</c> and <c>hasDiscount</c> are external, populated by the
/// gateway when projecting <c>@requires</c> dependencies.
/// </summary>
public sealed class Product
{
    public string Id { get; set; } = default!;

    public double? Price { get; set; }

    public bool? HasDiscount { get; set; }
}
