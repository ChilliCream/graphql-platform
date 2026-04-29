namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Price;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>price</c> subgraph
/// (<c>@key(fields: "id pid category { id tag }")</c>).
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public string? Pid { get; init; }

    public Category? Category { get; init; }

    public Price? Price { get; init; }
}
