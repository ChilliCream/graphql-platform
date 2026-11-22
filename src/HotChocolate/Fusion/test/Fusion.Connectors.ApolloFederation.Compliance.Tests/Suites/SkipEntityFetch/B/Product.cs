namespace HotChocolate.Fusion.Suites.SkipEntityFetch.B;

public sealed class Product
{
    public string Id { get; init; } = default!;
    public ProductDetails? Details { get; init; }
    public ProductInventory? Inventory { get; init; }
}
