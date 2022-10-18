using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// The collection segment represents one page of a pageable dataset / collection.
/// </summary>
public class CollectionSegment : IPage
{
    private readonly Func<CancellationToken, ValueTask<int>> _getTotalCount;

    /// <summary>
    /// Initializes <see cref="CollectionSegment" />.
    /// </summary>
    /// <param name="getItems">
    /// A delegate to request the items that belong to this page.
    /// </param>
    /// <param name="info">
    /// Additional information about this page.
    /// </param>
    /// <param name="getTotalCount">
    /// A delegate to request the the total count.
    /// </param>
    public CollectionSegment(
        Func<CancellationToken, ValueTask<IReadOnlyCollection<object>>> getItems,
        CollectionSegmentInfo info,
        Func<CancellationToken, ValueTask<int>> getTotalCount)
    {
        _getItems = getItems ??
            throw new ArgumentNullException(nameof(getItems));
        Info = info ??
            throw new ArgumentNullException(nameof(info));
        _getTotalCount = getTotalCount ??
            throw new ArgumentNullException(nameof(getTotalCount));
    }

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
    /// The total count of the data set / collection that is being paged.
    /// </param>
    public CollectionSegment(
        IReadOnlyCollection<object> items,
        CollectionSegmentInfo info,
        int totalCount = 0)
    {
        _getTotalCount = _ => new(totalCount);
        _getItems = _ => new(items);
        Info = info ??
            throw new ArgumentNullException(nameof(info));
    }


    private readonly Func<CancellationToken, ValueTask<IReadOnlyCollection<object>>> _getItems;

    /// <summary>
    /// Requests the items that belong to this page.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// the items that belong to this page.
    /// </returns>
    public ValueTask<IReadOnlyCollection<object>> GetItemsAsync(CancellationToken cancellationToken) => _getItems(cancellationToken);

    /// <summary>
    /// Gets more information about this page.
    /// </summary>
    public CollectionSegmentInfo Info { get; }

    /// <summary>
    /// Gets more information about this page.
    /// </summary>
    IPageInfo IPage.Info => Info;

    /// <summary>
    /// Requests the total count of the data set / collection that is being paged.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// The total count of the data set / collection.
    /// </returns>
    public ValueTask<int> GetTotalCountAsync(CancellationToken cancellationToken) =>
        _getTotalCount(cancellationToken);
}
