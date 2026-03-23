using System.Collections;
using System.Collections.Immutable;

namespace GreenDonut.Data;

/// <summary>
/// Represents a page of a result set.
/// </summary>
/// <typeparam name="T">
/// The type of the items.
/// </typeparam>
public abstract class Page<T> : IReadOnlyList<T>
{
    private ImmutableArray<T>? _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="Page{T}"/> class.
    /// </summary>
    /// <param name="entries">
    /// The entries of the page.
    /// </param>
    /// <param name="hasNextPage">
    /// Defines if there is a next page.
    /// </param>
    /// <param name="hasPreviousPage">
    /// Defines if there is a previous page.
    /// </param>
    /// <param name="totalCount">
    /// The total count of items in the dataset.
    /// </param>
    /// <param name="items">
    /// The items of the page. If null, items will be derived from entries on first access.
    /// </param>
    protected Page(
        ImmutableArray<PageEntry<T>> entries,
        bool hasNextPage,
        bool hasPreviousPage,
        int? totalCount = null,
        ImmutableArray<T>? items = null)
    {
        _items = items;
        Entries = entries;
        HasNextPage = hasNextPage;
        HasPreviousPage = hasPreviousPage;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Page{T}"/> class.
    /// </summary>
    /// <param name="entries">
    /// The entries of the page.
    /// </param>
    /// <param name="hasNextPage">
    /// Defines if there is a next page.
    /// </param>
    /// <param name="hasPreviousPage">
    /// Defines if there is a previous page.
    /// </param>
    /// <param name="index">
    /// The index number of this page.
    ///</param>
    /// <param name="requestedPageSize">
    /// The requested page size.
    /// </param>
    /// <param name="totalCount">
    /// The total count of items in the dataset.
    /// </param>
    /// <param name="items">
    /// The items of the page. If null, items will be derived from entries on first access.
    /// </param>
    protected Page(
        ImmutableArray<PageEntry<T>> entries,
        bool hasNextPage,
        bool hasPreviousPage,
        int index,
        int requestedPageSize,
        int totalCount,
        ImmutableArray<T>? items = null)
    {
        _items = items;
        Entries = entries;
        HasNextPage = hasNextPage;
        HasPreviousPage = hasPreviousPage;
        Index = index;
        RequestedSize = requestedPageSize;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Gets the entries of this page.
    /// </summary>
    public ImmutableArray<PageEntry<T>> Entries { get; }

    /// <summary>
    /// Gets the items of this page.
    /// </summary>
    public ImmutableArray<T> Items => _items ??= CreateItemsFromEntries(Entries);

    /// <summary>
    /// Gets the number of items in this page.
    /// </summary>
    public int Count => Entries.Length;

    /// <summary>
    /// Gets the item at the specified index.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the item.
    /// </param>
    public T this[int index] => Entries[index].Item;

    /// <summary>
    /// Gets the first entry of this page.
    /// </summary>
    public PageEntry<T>? First => Entries.Length > 0 ? Entries[0] : null;

    /// <summary>
    /// Gets the last entry of this page.
    /// </summary>
    public PageEntry<T>? Last => Entries.Length > 0 ? Entries[^1] : null;

    /// <summary>
    /// Defines if there is a next page.
    /// </summary>
    public bool HasNextPage { get; }

    /// <summary>
    /// Defines if there is a previous page.
    /// </summary>
    public bool HasPreviousPage { get; }

    /// <summary>
    /// Gets the index number of this page.
    /// </summary>
    public int? Index { get; }

    /// <summary>
    /// Gets the requested page size.
    /// This value can be null if the page size is unknown.
    /// </summary>
    internal int? RequestedSize { get; }

    /// <summary>
    /// Gets the total count of items in the dataset.
    /// This value can be null if the total count is unknown.
    /// </summary>
    public int? TotalCount { get; }

    /// <summary>
    /// Creates a cursor for an entry of this page.
    /// </summary>
    /// <param name="entry">
    /// The entry for which a cursor shall be created.
    /// </param>
    /// <returns>
    /// Returns a cursor for the entry.
    /// </returns>
    public string CreateCursor(PageEntry<T> entry)
    {
        EnsureIndex(entry.Index);
        return CreateCursor(entry.Index, 0, 0, 0);
    }

    /// <summary>
    /// Creates a relative cursor for an entry of this page.
    /// </summary>
    /// <param name="entry">
    /// The entry for which a cursor shall be created.
    /// </param>
    /// <param name="offset">
    /// The offset relative to the current cursor position.
    /// </param>
    /// <returns>
    /// Returns a cursor for the entry.
    /// </returns>
    public string CreateCursor(PageEntry<T> entry, int offset)
    {
        EnsureIndex(entry.Index);

        if (Index is null || TotalCount is null)
        {
            throw new InvalidOperationException("This page does not allow relative cursors.");
        }

        return CreateCursor(entry.Index, offset, Index.Value, TotalCount.Value);
    }

    /// <summary>
    /// An empty page.
    /// </summary>
    public static Page<T> Empty => ValueCursorPage<T>.Empty;

    protected abstract string CreateCursor(int index, int offset, int pageIndex, int totalCount);

    private void EnsureIndex(int index)
    {
        if ((uint)index >= (uint)Entries.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    /// <summary>
    /// Gets the enumerator for the items of this page.
    /// </summary>
    public Enumerator GetEnumerator() => new(Entries);

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        foreach (var entry in Entries)
        {
            yield return entry.Item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => ((IEnumerable<T>)this).GetEnumerator();

    /// <summary>
    /// Creates a page whose cursor can be created directly from the page item.
    /// </summary>
    public static Page<T> Create(
        ImmutableArray<T> items,
        bool hasNextPage,
        bool hasPreviousPage,
        Func<T, string> createCursor,
        int? totalCount = null)
        => new ValueCursorPage<T>(ToEntries(items), hasNextPage, hasPreviousPage, createCursor, totalCount, items);

    /// <summary>
    /// Creates a page whose cursor can be created directly from the page item.
    /// </summary>
    public static Page<T> Create(
        ImmutableArray<T> items,
        bool hasNextPage,
        bool hasPreviousPage,
        Func<EdgeEntry<T>, string> createCursor,
        int index,
        int requestedPageSize,
        int totalCount)
        => new ValueCursorPage<T>(
            ToEntries(items),
            hasNextPage,
            hasPreviousPage,
            createCursor,
            index,
            requestedPageSize,
            totalCount,
            items);

    /// <summary>
    /// Creates a page whose cursor must be created from a different source element than the page item.
    /// </summary>
    public static Page<T> Create<TElement>(
        ImmutableArray<T> items,
        ImmutableArray<TElement> elements,
        bool hasNextPage,
        bool hasPreviousPage,
        Func<TElement, string> createCursor,
        int? totalCount = null)
        => new ElementCursorPage<TElement, T>(
            ToEntries(items),
            elements,
            hasNextPage,
            hasPreviousPage,
            createCursor,
            totalCount,
            items);

    /// <summary>
    /// Creates a page whose cursor must be created from a different source element than the page item.
    /// </summary>
    public static Page<T> Create<TElement>(
        ImmutableArray<T> items,
        ImmutableArray<TElement> elements,
        bool hasNextPage,
        bool hasPreviousPage,
        Func<EdgeEntry<TElement>, string> createCursor,
        int index,
        int requestedPageSize,
        int totalCount)
        => new ElementCursorPage<TElement, T>(
            ToEntries(items),
            elements,
            hasNextPage,
            hasPreviousPage,
            createCursor,
            index,
            requestedPageSize,
            totalCount,
            items);

    internal static ImmutableArray<PageEntry<T>> ToEntries(ImmutableArray<T> items)
    {
        if (items.IsEmpty)
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<PageEntry<T>>(items.Length);

        for (var i = 0; i < items.Length; i++)
        {
            builder.Add(new PageEntry<T>(items[i], i));
        }

        return builder.MoveToImmutable();
    }

    private static ImmutableArray<T> CreateItemsFromEntries(ImmutableArray<PageEntry<T>> entries)
    {
        if (entries.IsEmpty)
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<T>(entries.Length);

        for (var i = 0; i < entries.Length; i++)
        {
            builder.Add(entries[i].Item);
        }

        return builder.MoveToImmutable();
    }

    /// <summary>
    /// A struct-based enumerator that yields the nodes of the page entries.
    /// </summary>
    public struct Enumerator
    {
        private readonly ImmutableArray<PageEntry<T>> _entries;
        private int _index;

        internal Enumerator(ImmutableArray<PageEntry<T>> entries)
        {
            _entries = entries;
            _index = -1;
        }

        /// <summary>
        /// Gets the current item.
        /// </summary>
        public T Current => _entries[_index].Item;

        /// <summary>
        /// Advances the enumerator to the next item.
        /// </summary>
        public bool MoveNext() => ++_index < _entries.Length;
    }
}
