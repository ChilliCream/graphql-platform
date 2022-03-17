namespace HotChocolate.Execution;

/// <summary>
/// Specifies the kind of execution result.
/// </summary>
public enum ExecutionResultKind
{
    /// <summary>
    /// A single result.
    /// </summary>
    SingleResult,

    /// <summary>
    /// A stream of results.
    /// </summary>
    StreamResult
}
