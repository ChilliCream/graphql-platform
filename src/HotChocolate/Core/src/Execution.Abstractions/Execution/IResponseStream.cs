namespace HotChocolate.Execution;

/// <summary>
/// The response stream represents a stream of <see cref="IOperationResult" /> that are produced
/// by the execution engine.
/// </summary>
public interface IResponseStream : IExecutionResult
{
    /// <summary>
    /// Reads the result stream.
    /// </summary>
    IAsyncEnumerable<IOperationResult> ReadResultsAsync();
}
