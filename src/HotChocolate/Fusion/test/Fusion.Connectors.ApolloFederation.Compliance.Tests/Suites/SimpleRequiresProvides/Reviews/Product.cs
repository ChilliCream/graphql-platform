namespace HotChocolate.Fusion.Suites.SimpleRequiresProvides.Reviews;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>reviews</c> subgraph
/// (<c>@key(fields: "upc")</c>). The reviews subgraph contributes the
/// <c>reviews</c> field; <c>name</c>, <c>price</c>, and <c>weight</c> are
/// owned elsewhere.
/// </summary>
public sealed class Product
{
    public string Upc { get; init; } = default!;
}
