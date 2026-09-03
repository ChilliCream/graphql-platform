namespace HotChocolate.Execution;

/// <summary>
/// Represents the completion of a pending incremental delivery operation in a GraphQL response.
/// Indicates that all data associated with the corresponding pending result has been delivered.
/// </summary>
/// <param name="Id">The request unique pending data identifier that matches a prior pending result.</param>
/// <param name="Errors">
/// Field errors that caused the incremental delivery to fail due to error bubbling above the incremental result's path.
/// When present, indicates the delivery has failed.
/// </param>
/// <param name="Extensions">Additional information associated with this completed result, if present.</param>
public sealed record CompletedResult(
    int Id,
    IReadOnlyList<IError>? Errors = null,
    IReadOnlyDictionary<string, object?>? Extensions = null)
{
    /// <summary>
    /// Gets the request unique pending data identifier that matches a prior pending result.
    /// </summary>
    public int Id { get; init; } = Id;

    /// <summary>
    /// Gets field errors that caused the incremental delivery to fail due to error bubbling
    /// above the incremental result's path. When present, indicates the delivery has failed.
    /// </summary>
    public IReadOnlyList<IError>? Errors { get; init; } = Errors;

    /// <summary>
    /// Gets additional information associated with this completed result, or <c>null</c> when none is present.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Extensions { get; init; } = Extensions;
}
