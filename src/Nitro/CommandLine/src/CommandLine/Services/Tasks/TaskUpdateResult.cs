namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// The outcome of <see cref="ITaskStore.UpdateTaskAsync"/>.
/// </summary>
internal sealed record TaskUpdateResult
{
    /// <summary>
    /// The database column names that changed value.
    /// </summary>
    public IReadOnlyList<string> ChangedFields { get; init; } = [];
}
