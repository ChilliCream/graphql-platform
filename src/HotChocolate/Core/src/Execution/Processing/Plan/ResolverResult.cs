namespace HotChocolate.Execution.Processing.Plan;

/// <summary>
/// The resolver result form an async resolver.
/// </summary>
public readonly struct ResolverResult
{
    internal ResolverResult(
        ISelection selection,
        Path path,
        ExecutionTaskStatus status,
        object? result)
    {
        Selection = selection;
        Path = path;
        Status = status;
        Result = result;
    }

    /// <summary>
    /// Gets the executed selection.
    /// </summary>
    public ISelection Selection { get; }

    /// <summary>
    /// Gets the execution path.
    /// </summary>
    public Path Path { get; }

    /// <summary>
    /// Gets the execution status.
    /// </summary>
    public ExecutionTaskStatus Status { get; }

    /// <summary>
    /// Gets the resolver result.
    /// </summary>
    public object? Result { get; }
}
