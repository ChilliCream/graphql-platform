namespace HotChocolate.Fusion.Suites.CorruptedSupergraphNodeId.A;

/// <summary>
/// The <c>Chat</c> entity as projected by subgraph <c>a</c>.
/// The <c>id</c> field is external; this subgraph contributes the
/// <c>account</c> relationship.
/// </summary>
public sealed class Chat : INode
{
    public string Id { get; init; } = default!;
    public string AccountId { get; init; } = default!;
}
