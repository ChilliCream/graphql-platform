namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// The outcome of <see cref="ITaskStore.CreateTaskAsync"/>.
/// </summary>
internal sealed record TaskCreationResult
{
    public required string Id { get; init; }

    /// <summary>
    /// The IDs of tasks that block the new task, per the same rules as
    /// ComputeBlockedAsync.
    /// </summary>
    public IReadOnlyList<string> BlockedBy { get; init; } = [];
}
