namespace HotChocolate.Fusion.Suites.MysteriousExternal.Price;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>price</c> subgraph
/// (<c>extend type Product @key(fields: "id")</c>). Owns <c>price</c>;
/// <c>id</c> is external.
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public double? Price { get; init; }
}
