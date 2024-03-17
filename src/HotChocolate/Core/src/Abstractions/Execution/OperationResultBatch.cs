using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Properties;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a batch of operation results.
/// </summary>
public sealed class OperationResultBatch : ExecutionResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="OperationResultBatch"/>.
    /// </summary>
    /// <param name="results">
    /// The results of this batch.
    /// </param>
    /// <param name="contextData">
    /// The result context data which represent additional properties that are NOT written to the transport.
    /// </param>
    /// <exception cref="ArgumentException">
    /// The result must be either an operation result or a response stream.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="results"/> is <c>null</c>.
    /// </exception>
    public OperationResultBatch(
        IReadOnlyList<IExecutionResult> results,
        IReadOnlyDictionary<string, object?>? contextData = null) 
        : base(cleanupTasks: [() => RunCleanUp(results),])
    {
        foreach (var result in results)
        {
            if (result is not IResponseStream and not OperationResult)
            {
                throw new ArgumentException(
                    AbstractionResources.OperationResultBatch_ResponseStreamOrOperationResult,
                    nameof(results));
            }
        }
        
        Results = results ?? throw new ArgumentNullException(nameof(results));
        ContextData = contextData;
    }
    
    /// <summary>
    /// Gets the result kind.
    /// </summary>
    public override ExecutionResultKind Kind => ExecutionResultKind.BatchResult;

    /// <summary>
    /// Gets the results of this batch.
    /// </summary>
    public IReadOnlyList<IExecutionResult> Results { get; }

    /// <summary>
    /// Gets the result context data which represent additional
    /// properties that are NOT written to the transport.
    /// </summary>
    public override IReadOnlyDictionary<string, object?>? ContextData { get; }
    
    private static async ValueTask RunCleanUp(IReadOnlyList<IExecutionResult> results)
    {
        foreach (var result in results)
        {
            await result.DisposeAsync().ConfigureAwait(false);
        }
    }
}