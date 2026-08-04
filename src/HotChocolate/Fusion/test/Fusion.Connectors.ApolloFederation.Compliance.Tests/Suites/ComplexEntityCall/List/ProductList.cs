namespace HotChocolate.Fusion.Suites.ComplexEntityCall.List;

/// <summary>
/// The <c>ProductList</c> entity as projected by the <c>list</c> subgraph
/// (<c>@key(fields: "products { id pid }")</c>). Owns the shareable
/// <c>first</c> and <c>selected</c> fields.
/// </summary>
public sealed class ProductList
{
    public IReadOnlyList<Product> Products { get; init; } = [];

    public Product? First { get; init; }

    public Product? Selected { get; init; }
}
