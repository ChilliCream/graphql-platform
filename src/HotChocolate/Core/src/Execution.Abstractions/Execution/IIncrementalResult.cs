namespace HotChocolate.Execution;

/// <summary>
/// Represents an incremental result that delivers data for a @defer or @stream directive.
/// </summary>
public interface IIncrementalResult
{
    /// <summary>
    /// Gets the request unique pending data identifier that matches a prior pending result.
    /// </summary>
    uint Id { get; }

    /// <summary>
    /// Gets field errors that occurred during execution of this incremental result.
    /// Only includes errors that did not bubble above the incremental result's path.
    /// </summary>
    IReadOnlyList<IError>? Errors { get; }
}
