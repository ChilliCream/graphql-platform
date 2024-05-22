using System.Collections.Generic;

namespace HotChocolate.Execution;

/// <summary>
/// The response stream represents a stream of <see cref="IOperationResult" /> that are produced
/// by the execution engine.
/// </summary>
public interface IResponseStream : IExecutionResult
{
    /// <summary>
    /// Reads the subscription results from the execution engine.
    /// </summary>
    IAsyncEnumerable<IOperationResult> ReadResultsAsync();
}
