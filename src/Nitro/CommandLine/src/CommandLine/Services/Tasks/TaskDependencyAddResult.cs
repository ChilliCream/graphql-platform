namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// The outcome of <see cref="ITaskStore.AddDependencyAsync"/>.
/// </summary>
internal sealed record TaskDependencyAddResult
{
    /// <summary>
    /// Null for successful additions; an edge creating a blocking cycle is rejected
    /// with <see cref="ExitException"/>.
    /// </summary>
    public IReadOnlyList<string>? Cycle { get; init; }
}
