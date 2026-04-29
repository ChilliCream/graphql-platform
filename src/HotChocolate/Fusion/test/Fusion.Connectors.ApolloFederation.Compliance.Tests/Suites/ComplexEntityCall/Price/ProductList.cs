namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Price;

/// <summary>
/// The <c>ProductList</c> entity as projected by the <c>price</c> subgraph
/// (<c>@key(fields: "products { id pid category { id tag } } selected { id }")</c>).
/// Owns the shareable <c>first</c> and <c>selected</c> fields.
/// </summary>
public sealed class ProductList
{
    public IReadOnlyList<Product> Products { get; init; } = [];

    public Product? First { get; init; }

    public Product? Selected { get; init; }
}
