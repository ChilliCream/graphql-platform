namespace HotChocolate.Fusion.Suites.IncludeSkip.B;

/// <summary>
/// The <c>Product</c> entity in the <c>b</c> subgraph
/// (<c>@key(fields: "id")</c>). Owns <c>isExpensive</c>; <c>price</c>
/// is external.
/// </summary>
public sealed class Product
{
    public string Id { get; set; } = default!;

    public double? Price { get; set; }
}
