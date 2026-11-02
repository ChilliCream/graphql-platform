namespace HotChocolate.Fusion.Suites.NestedProvides.AllProducts;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>all-products</c>
/// subgraph (<c>@key(fields: "id")</c>). This subgraph only owns the
/// key; all other fields are contributed by other subgraphs.
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;
}
