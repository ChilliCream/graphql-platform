namespace HotChocolate.Fusion.Suites.SharedRoot.Price;

/// <summary>
/// The <c>Price</c> value type owned by the <c>price</c> subgraph.
/// </summary>
public sealed class Price
{
    public string Id { get; init; } = default!;

    public int Amount { get; init; }

    public string Currency { get; init; } = default!;
}
