namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// The outcome of <see cref="ITaskStore.AddDependencyAsync"/>.
/// </summary>
internal sealed record TaskDependencyAddResult
{
    /// <summary>
    /// Always null: a new edge that would close a blocking-dependency cycle
    /// is rejected with an <see cref="ExitException"/> before it commits, so
    /// a successful call never reports one.
    /// </summary>
    public IReadOnlyList<string>? Cycle { get; init; }
}
