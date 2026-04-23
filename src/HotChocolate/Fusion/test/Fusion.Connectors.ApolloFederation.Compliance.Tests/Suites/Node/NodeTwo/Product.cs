namespace HotChocolate.Fusion.Suites.Node.NodeTwo;

/// <summary>
/// The <c>Product</c> projection used by the <c>node-two</c> subgraph.
/// </summary>
public sealed class Product : INode
{
    public string Id { get; init; } = default!;
}
