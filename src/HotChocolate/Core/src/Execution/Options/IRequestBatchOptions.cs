
namespace HotChocolate.Execution.Options;

/// <summary>
/// Represents a dedicated options accessor to read the configured batching options.
/// </summary>
public interface IRequestBatchOptions
{
    /// <summary>
    /// The maximum amount of queries from a single batch that will be executed in parallel for a single batched request.
    /// This value is ignored if allowParallelExecution is false in IRequestExecutor.ExecuteBatchAsync.
    /// This will NOT limit the total amount of concurrent batched queries accross requests.
    /// </summary>
    /// <see cref="IRequestExecutor.ExecuteBatchAsync"/>
    public int MaxConcurrentBatchQueries { get; } 
}
