using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Pagination
{
    /// <inheritdoc />
    public class CollectionSegment<T> : CollectionSegment
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
            : base(new CollectionWrapper(items), info, getTotalCount)
        {
            Items = items;
        }

        /// <summary>
        /// The items that belong to this page.
        /// </summary>
        public new IReadOnlyCollection<T> Items { get; }

        private class CollectionWrapper : IReadOnlyCollection<object>
        {
            private readonly IReadOnlyCollection<T> _collection;

            public CollectionWrapper(IReadOnlyCollection<T> collection)
            {
                _collection = collection;
            }

            public IEnumerator<object> GetEnumerator()
            {
                foreach (T element in _collection)
                {
                    yield return element;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int Count => _collection.Count;

        }
    }
}
