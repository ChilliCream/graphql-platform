using System.Collections.Immutable;

namespace HotChocolate.Execution;

/// <summary>
/// Represents an incremental result that delivers additional fields for a @defer directive.
/// </summary>
public sealed class IncrementalObjectResult : IIncrementalResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="IncrementalObjectResult"/>.
    /// </summary>
    /// <param name="id">
    /// The unique identifier that correlates this result with its pending entry.
    /// </param>
    /// <param name="errors">
    /// The GraphQL errors that occurred while resolving the deferred fragment.
    /// </param>
    /// <param name="subPath">
    /// The sub-path to concatenate with the pending result's path, or <c>null</c>
    /// if the path is the same as the pending result's path.
    /// </param>
    /// <param name="data">
    /// The additional response fields to merge into the deferred fragment location.
    /// </param>
    public IncrementalObjectResult(
        int id,
        ImmutableList<IError>? errors = null,
        Path? subPath = null,
        OperationResultData? data = null)
    {
        Id = id;
        Errors = errors ?? [];
        SubPath = subPath;
        Data = data;
    }

    /// <summary>
    /// Gets the unique identifier that correlates this incremental result with
    /// its corresponding pending entry.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the GraphQL errors that occurred while resolving the deferred fragment.
    /// </summary>
    public ImmutableList<IError> Errors { get; }

    /// <summary>
    /// Gets the sub-path that is concatenated with the pending result's path to determine
    /// the final path for this incremental data. When <c>null</c>, the path is the same
    /// as the pending result's path.
    /// </summary>
    public Path? SubPath { get; }

    /// <summary>
    /// Gets the additional response fields to merge into the deferred fragment location.
    /// </summary>
    public OperationResultData? Data { get; }
}
