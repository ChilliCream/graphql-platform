namespace HotChocolate.Execution;

/// <summary>
/// Represents an incremental result that delivers additional fields for a @defer directive.
/// </summary>
public interface IIncrementalObjectResult : IIncrementalResult
{
    /// <summary>
    /// Gets the sub-path that is concatenated with the pending result's path to determine
    /// the final path for this incremental data. When <c>null</c>, the path is the same
    /// as the pending result's path.
    /// </summary>
    Path? SubPath { get; }

    /// <summary>
    /// Gets the additional response fields to merge into the deferred fragment location.
    /// </summary>
    object? Data { get; }
}
