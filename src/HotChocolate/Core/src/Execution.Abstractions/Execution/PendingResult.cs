namespace HotChocolate.Execution;

/// <summary>
/// Represents a pending incremental delivery operation in a GraphQL response.
/// </summary>
/// <param name="Id">The request unique pending data identifier.</param>
/// <param name="Path">
/// The path in the response where the incremental data will be delivered.
/// For @stream: indicates the list field that is not complete.
/// For @defer: indicates where the deferred fragment fields will be added.
/// </param>
/// <param name="Label">The label from the @defer or @stream directive's label argument, if present.</param>
public sealed record PendingResult(int Id, Path Path, string? Label = null)
{
    /// <summary>
    /// Gets the request unique pending data identifier.
    /// </summary>
    public int Id { get; init; } = Id;

    /// <summary>
    /// Gets the path in the response where the incremental data will be delivered.
    /// For @stream: indicates the list field that is not complete.
    /// For @defer: indicates where the deferred fragment fields will be added.
    /// </summary>
    public Path Path { get; init; } = Path;

    /// <summary>
    /// Gets the label from the @defer or @stream directive's label argument, if present.
    /// </summary>
    public string? Label { get; init; } = Label;
}
