namespace HotChocolate.Fusion.Suites.RequiresWithArgument.A;

/// <summary>
/// The <c>Product</c> entity in the <c>a</c> subgraph
/// (<c>@key(fields: "upc")</c>). Owns <c>shippingEstimate</c> and
/// <c>isExpensiveCategory</c>; <c>weight</c>, <c>price</c>, and
/// <c>category</c> are external and supplied by the federation
/// external setter from the entity representation.
/// </summary>
public sealed class Product
{
    public string Upc { get; set; } = default!;

    public int? Weight { get; set; }

    public int? Price { get; set; }

    public Category? Category { get; set; }
}
