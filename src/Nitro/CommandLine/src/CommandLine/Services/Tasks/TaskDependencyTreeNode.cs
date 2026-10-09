namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// A dependency-tree node; null status and title mean the task is missing.
/// A repeated node has already appeared in the tree and has no expanded children.
/// </summary>
internal sealed record TaskDependencyTreeNode
{
    public required string Id { get; init; }
    public string? Type { get; init; }
    public string? Status { get; init; }
    public string? Title { get; init; }
    public required bool Repeated { get; init; }
    public required IReadOnlyList<TaskDependencyTreeNode> Children { get; init; }
}
