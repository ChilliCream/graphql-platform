namespace HotChocolate.Execution;

/// <summary>
/// The response stream represents a stream of <see cref="OperationResult" /> that are produced
/// by the execution engine.
/// </summary>
public interface IResponseStream : IExecutionResult
{
    /// <summary>
    /// Reads the result stream.
    /// </summary>
    IAsyncEnumerable<OperationResult> ReadResultsAsync();
}
