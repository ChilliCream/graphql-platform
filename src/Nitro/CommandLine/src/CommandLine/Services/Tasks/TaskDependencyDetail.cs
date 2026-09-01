namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// One outgoing dependency edge from a task, joined with the target task's
/// current status and title, as returned by
/// <see cref="ITaskStore.GetDependenciesAsync"/>. Null <see cref="Status"/>
/// and <see cref="Title"/> mean the target task no longer exists.
/// </summary>
internal sealed record TaskDependencyDetail
{
    public required string Type { get; init; }
    public required string DependsOnId { get; init; }
    public string? Status { get; init; }
    public string? Title { get; init; }
}
