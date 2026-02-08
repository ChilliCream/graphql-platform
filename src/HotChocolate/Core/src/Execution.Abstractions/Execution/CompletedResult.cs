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
public sealed record CompletedResult(uint Id, IReadOnlyList<IError>? Errors = null)
{
    /// <summary>
    /// Gets the request unique pending data identifier that matches a prior pending result.
    /// </summary>
    public uint Id { get; init; } = Id;

    /// <summary>
    /// Gets field errors that caused the incremental delivery to fail due to error bubbling
    /// above the incremental result's path. When present, indicates the delivery has failed.
    /// </summary>
    public IReadOnlyList<IError>? Errors { get; init; } = Errors;
}
