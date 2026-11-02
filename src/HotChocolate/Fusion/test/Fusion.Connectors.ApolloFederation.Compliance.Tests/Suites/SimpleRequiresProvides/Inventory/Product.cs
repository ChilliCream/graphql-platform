namespace HotChocolate.Fusion.Suites.SimpleRequiresProvides.Inventory;

/// <summary>
/// The <c>Product</c> entity in the <c>inventory</c> subgraph
/// (<c>@key(fields: "upc")</c>). Owns <c>inStock</c>, <c>shippingEstimate</c>,
/// and <c>shippingEstimateTag</c>; <c>weight</c> and <c>price</c> are
/// external and supplied by the federation external setter when the
/// gateway attaches the requires dependencies to the entity representation.
/// </summary>
public sealed class Product
{
    public string Upc { get; set; } = default!;

    public int? Weight { get; set; }

    public int? Price { get; set; }
}
