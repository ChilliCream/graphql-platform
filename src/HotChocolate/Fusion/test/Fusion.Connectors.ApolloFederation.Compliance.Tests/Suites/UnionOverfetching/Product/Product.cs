namespace HotChocolate.Fusion.Suites.UnionOverfetching.Product;

public sealed class Product
{
    public string Id { get; init; } = default!;
    public string Kind { get; init; } = default!;
    public string? A { get; init; }
    public string? B { get; init; }
    public string? C { get; init; }
}
