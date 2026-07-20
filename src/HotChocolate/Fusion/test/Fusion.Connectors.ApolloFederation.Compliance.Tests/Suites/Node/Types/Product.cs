namespace HotChocolate.Fusion.Suites.Node.Types;

/// <summary>
/// The <c>Product</c> projection used by the <c>types</c> subgraph.
/// Owns <c>name</c> and <c>price</c> in addition to the federated key.
/// </summary>
public sealed class Product : INode
{
    public string Id { get; init; } = default!;

    public string Name { get; init; } = default!;

    public double Price { get; init; }
}
