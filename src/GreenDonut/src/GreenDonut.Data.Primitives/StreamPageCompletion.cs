namespace GreenDonut.Data;

/// <summary>
/// Describes the pagination facts known after a streamed page is fully enumerated.
/// </summary>
public sealed class StreamPageCompletion
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamPageCompletion"/> class.
    /// </summary>
    /// <param name="hasNextPage">
    /// Defines if there is a next page.
    /// </param>
    /// <param name="hasPreviousPage">
    /// Defines if there is a previous page.
    /// </param>
    /// <param name="startCursor">
    /// The cursor of the first enumerated item, or <see langword="null"/> when no items were enumerated.
    /// </param>
    /// <param name="endCursor">
    /// The cursor of the last enumerated item, or <see langword="null"/> when no items were enumerated.
    /// </param>
    public StreamPageCompletion(
        bool hasNextPage,
        bool hasPreviousPage,
        string? startCursor,
        string? endCursor)
    {
        HasNextPage = hasNextPage;
        HasPreviousPage = hasPreviousPage;
        StartCursor = startCursor;
        EndCursor = endCursor;
    }

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage { get; }

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage { get; }

    /// <summary>
    /// Gets the cursor of the first enumerated item.
    /// </summary>
    public string? StartCursor { get; }

    /// <summary>
    /// Gets the cursor of the last enumerated item.
    /// </summary>
    public string? EndCursor { get; }
}
