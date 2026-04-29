namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Price;

/// <summary>
/// Value type returned by <c>Product.price</c> in the <c>price</c> subgraph.
/// </summary>
public sealed class Price
{
    public float Value { get; init; }
}
