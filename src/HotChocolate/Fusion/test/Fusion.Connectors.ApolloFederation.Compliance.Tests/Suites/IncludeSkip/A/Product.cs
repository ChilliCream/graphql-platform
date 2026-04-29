namespace HotChocolate.Fusion.Suites.IncludeSkip.A;

/// <summary>
/// The <c>Product</c> entity in the <c>a</c> subgraph
/// (<c>@key(fields: "id")</c>). Owns <c>price</c>.
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public double Price { get; init; }
}
