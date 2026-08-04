namespace HotChocolate.Fusion.Suites.SimpleRequiresProvides.Products;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>products</c> subgraph
/// (<c>@key(fields: "upc")</c>). Owns <c>name</c>, <c>price</c>, and
/// <c>weight</c>.
/// </summary>
public sealed class Product
{
    public string Upc { get; init; } = default!;

    public string? Name { get; init; }

    public int? Price { get; init; }

    public int? Weight { get; init; }
}
