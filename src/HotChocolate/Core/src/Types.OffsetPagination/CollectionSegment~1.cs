using System.Buffers;
using System.Collections;

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
    /// <param name="totalCount">
    /// The total count of the data set / collection that is being paged.
    /// </param>
    public CollectionSegment(
        IReadOnlyList<T> items,
        CollectionSegmentInfo info,
        int totalCount)
        : base(new CollectionWrapper(items), info, totalCount)
    {
        Items = items;
    }

    /// <summary>
    /// The items that belong to this page.
    /// </summary>
    public new IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Accepts a page observer.
    /// </summary>
    public override void Accept(IPageObserver observer)
    {
        if(Items.Count == 0)
        {
            ReadOnlySpan<T> empty = Array.Empty<T>();
            observer.OnAfterSliced(empty, Info);
            return;
        }

        var buffer = ArrayPool<T>.Shared.Rent(Items.Count);

        for (var i = 0; i < Items.Count; i++)
        {
            buffer[i] = Items[i];
        }

        ReadOnlySpan<T> items = buffer.AsSpan(0, Items.Count);
        observer.OnAfterSliced(items, Info);

        buffer.AsSpan().Slice(0, Items.Count).Clear();
        ArrayPool<T>.Shared.Return(buffer);
    }

    /// <summary>
    /// This wrapper is used to be able to pass along the items collection to the base class
    /// which demands <see cref="IReadOnlyCollection{Object}"/>.
    /// </summary>
    private sealed class CollectionWrapper(IReadOnlyList<T> collection)
        : IReadOnlyList<object>
    {
        private readonly IReadOnlyList<T> _collection = collection
            ?? throw new ArgumentNullException(nameof(collection));

        public object this[int index] => _collection[index]!;

        public int Count => _collection.Count;

        public IEnumerator<object> GetEnumerator()
        {
            foreach (var element in _collection)
            {
                yield return element!;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
