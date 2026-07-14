namespace HotChocolate.Fusion.Suites.RequiresRequires.D;

/// <summary>
/// The <c>Product</c> entity in subgraph <c>d</c>.
/// <c>isExpensive</c> and <c>isExpensiveWithDiscount</c> are external,
/// populated by the gateway when projecting <c>@requires</c> dependencies.
/// </summary>
public sealed class Product
{
    public string Id { get; set; } = default!;

    public bool? IsExpensive { get; set; }

    public bool? IsExpensiveWithDiscount { get; set; }
}
