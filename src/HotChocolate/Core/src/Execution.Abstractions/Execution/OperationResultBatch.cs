using System.Collections.Immutable;

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
    /// <exception cref="ArgumentException">
    /// The result must be either an operation result or a response stream.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="results"/> is <c>null</c>.
    /// </exception>
    public OperationResultBatch(ImmutableList<IExecutionResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        foreach (var result in results)
        {
            if (result is not IResponseStream and not OperationResult)
            {
                throw new ArgumentException(
                    ExecutionAbstractionsResources.OperationResultBatch_ResponseStreamOrOperationResult,
                    nameof(results));
            }
        }

        Results = results ?? throw new ArgumentNullException(nameof(results));
        RegisterForCleanup(() => RunCleanUp(Results));
    }

    /// <summary>
    /// Gets the result kind.
    /// </summary>
    public override ExecutionResultKind Kind => ExecutionResultKind.BatchResult;

    /// <summary>
    /// Gets the results of this batch.
    /// </summary>
    public ImmutableList<IExecutionResult> Results { get; }

    private static async ValueTask RunCleanUp(IReadOnlyList<IExecutionResult> results)
    {
        foreach (var result in results)
        {
            await result.DisposeAsync().ConfigureAwait(false);
        }
    }
}
