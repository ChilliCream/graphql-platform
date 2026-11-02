namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// The outcome of <see cref="ITaskStore.CreateTaskAsync"/>.
/// </summary>
internal sealed record TaskCreationResult
{
    public required string Id { get; init; }

    /// <summary>
    /// Direct, non-parent blocking dependency ids whose targets are non-terminal.
    /// Use blocked or ready queries for the complete readiness result.
    /// </summary>
    public IReadOnlyList<string> BlockedBy { get; init; } = [];
}
