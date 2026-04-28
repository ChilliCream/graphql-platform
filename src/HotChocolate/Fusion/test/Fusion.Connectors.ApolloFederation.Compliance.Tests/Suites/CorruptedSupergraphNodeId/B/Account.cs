namespace HotChocolate.Fusion.Suites.CorruptedSupergraphNodeId.B;

/// <summary>
/// The <c>Account</c> entity as projected by subgraph <c>b</c>.
/// The <c>id</c> field is external; this subgraph contributes
/// <c>chats: [Chat!]!</c>.
/// </summary>
public sealed class Account : INode
{
    public string Id { get; init; } = default!;
}
