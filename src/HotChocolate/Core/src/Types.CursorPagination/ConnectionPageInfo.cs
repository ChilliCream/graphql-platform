namespace HotChocolate.Types.Pagination;

/// <summary>
/// Represents the connection paging page info.
/// This class provides additional information about pagination in a connection.
/// </summary>
public class ConnectionPageInfo : IPageInfo
{
    /// <summary>
    /// Initializes <see cref="ConnectionPageInfo" />.
    /// </summary>
    /// <param name="hasNextPage"></param>
    /// <param name="hasPreviousPage"></param>
    /// <param name="startCursor"></param>
    /// <param name="endCursor"></param>
    public ConnectionPageInfo(
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
    /// <c>true</c> if there is another page after the current one.
    /// <c>false</c> if this page is the last page of the current data set / collection.
    /// </summary>
    public bool HasNextPage { get; }

    /// <summary>
    /// <c>true</c> if there is before this page.
    /// <c>false</c> if this page is the first page in the current data set / collection.
    /// </summary>
    public bool HasPreviousPage { get; }

    /// <summary>
    /// When paginating backwards, the cursor to continue.
    /// </summary>
    public string? StartCursor { get; }

    /// <summary>
    /// When paginating forwards, the cursor to continue.
    /// </summary>
    public string? EndCursor { get; }

    public static ConnectionPageInfo Empty { get; } = new(false, false, null, null);
}
