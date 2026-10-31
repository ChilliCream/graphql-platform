namespace HotChocolate.Types.Pagination;

/// <summary>
/// Represents the offset paging info.
/// This class provides additional information about the selected page.
/// </summary>
public class CollectionSegmentInfo : IPageInfo
{
    private readonly bool _hasNextPage;
    private readonly bool _hasPreviousPage;

    /// <summary>
    /// Initializes <see cref="CollectionSegmentInfo" />.
    /// </summary>
    public CollectionSegmentInfo(
        bool hasNextPage,
        bool hasPreviousPage)
    {
        _hasNextPage = hasNextPage;
        _hasPreviousPage = hasPreviousPage;
    }

    /// <summary>
    /// <c>true</c> if there is another page after the current one.
    /// <c>false</c> if this page is the last page of the current data set / collection.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token that cancels the operation.
    /// </param>
    public ValueTask<bool> HasNextPageAsync(CancellationToken cancellationToken = default)
        => new(_hasNextPage);

    /// <summary>
    /// <c>true</c> if there is before this page.
    /// <c>false</c> if this page is the first page in the current data set / collection.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token that cancels the operation.
    /// </param>
    public ValueTask<bool> HasPreviousPageAsync(CancellationToken cancellationToken = default)
        => new(_hasPreviousPage);
}
