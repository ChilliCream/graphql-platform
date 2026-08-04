namespace HotChocolate.Fusion.Suites.Node.Node;

/// <summary>
/// The <c>Category</c> projection used by the <c>node</c> subgraph.
/// Carries only the federated key field.
/// </summary>
public sealed class Category : INode
{
    public string Id { get; init; } = default!;
}
