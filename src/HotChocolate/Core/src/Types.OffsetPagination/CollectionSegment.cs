namespace HotChocolate.Types.Pagination;

/// <summary>
/// The collection segment represents one page of a pageable dataset / collection.
/// </summary>
public class CollectionSegment : IPage
{
    /// <summary>
    /// Initializes <see cref="CollectionSegment" />.
    /// </summary>
    /// <param name="items">
    /// The items that belong to this page.
    /// </param>
    /// <param name="info">
    /// Additional information about this page.
    /// </param>
    /// <param name="totalCount">
    /// The totalCount.
    /// </param>
    public CollectionSegment(
        IReadOnlyList<object> items,
        CollectionSegmentInfo info,
        int totalCount)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        Info = info ?? throw new ArgumentNullException(nameof(info));
        TotalCount = totalCount;
    }

    /// <summary>
    /// The items that belong to this page.
    /// </summary>
    public IReadOnlyList<object> Items { get; }

    /// <summary>
    /// Gets more information about this page.
    /// </summary>
    public CollectionSegmentInfo Info { get; }

    /// <summary>
    /// Gets more information about this page.
    /// </summary>
    IPageInfo IPage.Info => Info;

    /// <summary>
    /// Returns the total count of the data set / collection that is being paged.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Accepts a page observer.
    /// </summary>
    public virtual void Accept(IPageObserver observer)
    {
    }
}
