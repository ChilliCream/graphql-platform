using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Pagination
{
    /// <summary>
    /// The collection segment represents one page of a pageable dataset / collection.
    /// </summary>
    public class CollectionSegment : IPage
    {
        private readonly Func<CancellationToken, ValueTask<int>> _getTotalCount;

        /// <summary>
        /// Initializes <see cref="CollectionSegment" />.
        /// </summary>
        /// <param name="items">
        /// The items that belong to this page.
        /// </param>
        /// <param name="info">
        /// Additional information about this page.
        /// </param>
        /// <param name="getTotalCount">
        /// A delegate to request the the total count.
        /// </param>
        public CollectionSegment(
            IReadOnlyCollection<object> items,
            CollectionSegmentInfo info,
            Func<CancellationToken, ValueTask<int>> getTotalCount)
        {
            Items = items ??
                throw new ArgumentNullException(nameof(items));
            Info = info ??
                throw new ArgumentNullException(nameof(info));
            _getTotalCount = getTotalCount ??
                throw new ArgumentNullException(nameof(getTotalCount));
        }

        /// <summary>
        /// The items that belong to this page.
        /// </summary>
        public IReadOnlyCollection<object> Items { get; }

        /// <summary>
        /// Gets more information about this page.
        /// </summary>
        public IPageInfo Info { get; }

        /// <summary>
        /// Requests the total count of the data set / collection that is being paged.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The total count of the data set / collection.
        /// </returns>
        public ValueTask<int> GetTotalCountAsync(
            CancellationToken cancellationToken) =>
            _getTotalCount(cancellationToken);
    }
}
