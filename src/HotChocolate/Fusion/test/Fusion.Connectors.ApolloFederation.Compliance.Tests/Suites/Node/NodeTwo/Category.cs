namespace HotChocolate.Fusion.Suites.Node.NodeTwo;

/// <summary>
/// The <c>Category</c> projection used by the <c>node-two</c> subgraph.
/// </summary>
public sealed class Category : INode
{
    public string Id { get; init; } = default!;
}
