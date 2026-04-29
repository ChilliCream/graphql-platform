namespace HotChocolate.Fusion.Suites.Node.Node;

/// <summary>
/// The <c>Product</c> projection used by the <c>node</c> subgraph.
/// Carries only the federated key field.
/// </summary>
public sealed class Product : INode
{
    public string Id { get; init; } = default!;
}
