namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// One node of a task's outgoing dependency tree, as returned by the
/// structured (JSON) output of <c>task dep tree</c>. Null <see cref="Status"/>
/// and <see cref="Title"/> mean the task no longer exists. A node whose
/// <see cref="Repeated"/> is <c>true</c> was already printed elsewhere in the
/// tree and is not expanded further.
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
