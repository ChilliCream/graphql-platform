using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// The collection segment represents one page of a pageable dataset / collection.
/// </summary>
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
        IReadOnlyCollection<T> items,
        CollectionSegmentInfo info,
        int totalCount = 0)
        : base(new CollectionWrapper(items), info, totalCount)
    {
        Items = items;
    }

    /// <summary>
    /// The items that belong to this page.
    /// </summary>
    public new IReadOnlyCollection<T> Items { get; }

    /// <summary>
    /// This wrapper is used to be able to pass along the items collection to the base class
    /// which demands <see cref="IReadOnlyCollection{Object}"/>.
    /// </summary>
    private sealed class CollectionWrapper : IReadOnlyCollection<object>
    {
        private readonly IReadOnlyCollection<T> _collection;

        public CollectionWrapper(IReadOnlyCollection<T> collection)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        public int Count => _collection.Count;

        public IEnumerator<object> GetEnumerator()
        {
            foreach (T element in _collection)
            {
                yield return element!;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
