namespace HotChocolate.Fusion.Suites.CorruptedSupergraphNodeId.A;

/// <summary>
/// The <c>Account</c> entity as projected by subgraph <c>a</c>.
/// </summary>
public sealed class Account : INode
{
    public string Id { get; init; } = default!;
    public string Username { get; init; } = default!;
}
