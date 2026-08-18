namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// The outcome of <see cref="ITaskStore.AddDependencyAsync"/>.
/// </summary>
internal sealed record TaskDependencyAddResult
{
    /// <summary>
    /// A blocking-dependency cycle the new edge closed, starting and ending
    /// at the dependent task, or null when none was found.
    /// </summary>
    public IReadOnlyList<string>? Cycle { get; init; }
}
