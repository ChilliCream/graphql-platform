using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Pagination
{
    /// <inheritdoc />
    public class CollectionSegment<T> : CollectionSegment
        where T : class
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
        /// <param name="getTotalCount">
        /// A delegate to request the the total count.
        /// </param>
        public CollectionSegment(
            IReadOnlyCollection<T> items,
            CollectionSegmentInfo info,
            Func<CancellationToken, ValueTask<int>> getTotalCount)
            : base(items, info, getTotalCount)
        {
            Items = items;
        }

        /// <summary>
        /// The items that belong to this page.
        /// </summary>
        public new IReadOnlyCollection<T> Items { get; }
    }
}
