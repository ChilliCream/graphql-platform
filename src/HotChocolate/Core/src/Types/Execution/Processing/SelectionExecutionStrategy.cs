namespace HotChocolate.Execution.Processing;

/// <summary>
/// Specifies how the selection is executed.
/// </summary>
public enum SelectionExecutionStrategy
{
    /// <summary>
    /// Defines that the default resolver pipeline shall be used.
    /// </summary>
    Default,

    /// <summary>
    /// Defines that the default resolver pipeline shall be used
    /// but that the resolver can only be executed serially.
    /// </summary>
    Serial,

    /// <summary>
    /// Defines that the selection has a side-effect free pure resolver.
    /// </summary>
    Pure,

    /// <summary>
    /// Defines that the selection uses a batch resolver pipeline that
    /// collects multiple parent contexts and resolves them in a single invocation.
    /// </summary>
    Batch
}
