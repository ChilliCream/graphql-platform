namespace HotChocolate.Fusion.Suites.Node.Types;

/// <summary>
/// The <c>Category</c> projection used by the <c>types</c> subgraph.
/// Owns <c>name</c> in addition to the federated key.
/// </summary>
public sealed class Category : INode
{
    public string Id { get; init; } = default!;

    public string Name { get; init; } = default!;
}
