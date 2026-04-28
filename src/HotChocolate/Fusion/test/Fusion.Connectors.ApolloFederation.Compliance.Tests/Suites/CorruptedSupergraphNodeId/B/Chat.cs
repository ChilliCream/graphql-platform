namespace HotChocolate.Fusion.Suites.CorruptedSupergraphNodeId.B;

/// <summary>
/// The <c>Chat</c> entity as projected by subgraph <c>b</c>.
/// This subgraph owns <c>id</c> and <c>text</c>.
/// </summary>
public sealed class Chat : INode
{
    public string Id { get; init; } = default!;
    public string AccountId { get; init; } = default!;
    public string Text { get; init; } = default!;
}
