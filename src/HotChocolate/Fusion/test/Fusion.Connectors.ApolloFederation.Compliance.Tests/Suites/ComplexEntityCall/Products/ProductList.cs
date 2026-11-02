namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Products;

/// <summary>
/// The <c>ProductList</c> entity as projected by the <c>products</c> subgraph
/// (<c>@key(fields: "products { id }")</c>).
/// </summary>
public sealed class ProductList
{
    public IReadOnlyList<Product> Products { get; init; } = [];
}
