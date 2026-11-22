namespace HotChocolate.Fusion.Suites.UnionOverfetching.Card;

public sealed class ProductCard
{
    public string Id { get; init; } = default!;
    public IReadOnlyList<object?>? Components { get; init; }
}
