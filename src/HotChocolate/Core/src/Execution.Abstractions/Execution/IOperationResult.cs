namespace HotChocolate.Execution;

/// <summary>
/// Represents a GraphQL operation result payload.
/// </summary>
public interface IOperationResult : IExecutionResult, IResultDataJsonFormatter
{
    /// <summary>
    /// Gets the index of the request that corresponds to this result.
    /// </summary>
    int? RequestIndex { get; }

    /// <summary>
    /// Gets the index of the variable set that corresponds to this result.
    /// </summary>
    int? VariableIndex { get; }

    /// <summary>
    /// A path to the insertion point that informs the client how to patch a
    /// subsequent delta payload into the original payload.
    /// </summary>
    Path? Path { get; }

    /// <summary>
    /// The data that is being delivered.
    /// </summary>
    object? Data { get; }

    /// <summary>
    /// Specifies if data was explicitly set.
    /// If <c>false</c> the data was not set (including null).
    /// </summary>
    bool IsDataSet { get; }

    /// <summary>
    /// Gets the GraphQL errors of the result.
    /// </summary>
    IReadOnlyList<IError>? Errors { get; }

    /// <summary>
    /// Gets the additional information that is passed along
    /// with the result and will be serialized for transport.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Extensions { get; }

    /// <summary>
    /// Gets the list of pending incremental delivery operations.
    /// Each pending result announces data that will be delivered incrementally in subsequent payloads.
    /// </summary>
    IReadOnlyList<PendingResult>? Pending { get; }

    /// <summary>
    /// Gets the list of incremental results containing data from @defer or @stream directives.
    /// Contains the actual data for previously announced pending operations.
    /// </summary>
    IReadOnlyList<IIncrementalResult>? Incremental { get; }

    /// <summary>
    /// Gets the list of completed incremental delivery operations.
    /// Each completed result indicates that all data for a pending operation has been delivered.
    /// </summary>
    IReadOnlyList<CompletedResult>? Completed { get; }

    /// <summary>
    /// Indicates whether more payloads will follow in the response stream.
    /// When <c>true</c>, clients should expect additional incremental data.
    /// When <c>false</c>, this is the final payload.
    /// </summary>
    bool HasNext { get; }
}
