namespace HotChocolate.Fusion.Suites.RequiresWithArgumentConflict.B;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>b</c> subgraph
/// (<c>@key(fields: "upc")</c>). Owns <c>name</c>, <c>price</c>,
/// <c>weight</c>, and <c>category</c>.
/// </summary>
public sealed class Product
{
    public string Upc { get; init; } = default!;

    public string? Name { get; init; }

    public int? Price { get; init; }

    public int? Weight { get; init; }

    public Category? Category { get; init; }
}
