using System.Collections;
using System.Collections.Immutable;

namespace GreenDonut.Data;

/// <summary>
/// Represents a page of a result set.
/// </summary>
/// <typeparam name="T">
/// The type of the items.
/// </typeparam>
public abstract class Page<T> : IEnumerable<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Page{T}"/> class.
    /// </summary>
    /// <param name="items">
    /// The items of the page.
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
    protected Page(
        ImmutableArray<T> items,
        bool hasNextPage,
        bool hasPreviousPage,
        int? totalCount = null)
    {
        Items = items;
        HasNextPage = hasNextPage;
        HasPreviousPage = hasPreviousPage;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Page{T}"/> class.
    /// </summary>
    /// <param name="items">
    /// The items of the page.
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
    protected Page(
        ImmutableArray<T> items,
        bool hasNextPage,
        bool hasPreviousPage,
        int index,
        int requestedPageSize,
        int totalCount)
    {
        Items = items;
        HasNextPage = hasNextPage;
        HasPreviousPage = hasPreviousPage;
        Index = index;
        RequestedSize = requestedPageSize;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Gets the items of this page.
    /// </summary>
    public ImmutableArray<T> Items { get; }

    /// <summary>
    /// Gets the first item of this page.
    /// </summary>
    public T? First => Items.Length > 0 ? Items[0] : default;

    /// <summary>
    /// Gets the zero-based index of the first item of this page.
    /// </summary>
    public int? FirstIndex => Items.Length > 0 ? 0 : null;

    /// <summary>
    /// Gets the last item of this page.
    /// </summary>
    public T? Last => Items.Length > 0 ? Items[^1] : default;

    /// <summary>
    /// Gets the zero-based index of the last item of this page.
    /// </summary>
    public int? LastIndex => Items.Length > 0 ? Items.Length - 1 : null;

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
    /// Creates a cursor for an item of this page.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the item for which a cursor shall be created.
    /// </param>
    /// <returns>
    /// Returns a cursor for the item.
    /// </returns>
    public string CreateCursor(int index)
    {
        EnsureIndex(index);
        return CreateCursor(index, 0, 0, 0);
    }

    public string CreateCursor(int index, int offset)
    {
        EnsureIndex(index);

        if (Index is null || TotalCount is null)
        {
            throw new InvalidOperationException("This page does not allow relative cursors.");
        }

        return CreateCursor(index, offset, Index.Value, TotalCount.Value);
    }

    /// <summary>
    /// An empty page.
    /// </summary>
    public static Page<T> Empty => ValueCursorPage<T>.Empty;

    protected abstract string CreateCursor(int index, int offset, int pageIndex, int totalCount);

    private void EnsureIndex(int index)
    {
        if ((uint)index >= (uint)Items.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    /// <summary>
    /// Gets the enumerator for the items of this page.
    /// </summary>
    public IEnumerator<T> GetEnumerator()
        => ((IEnumerable<T>)Items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <summary>
    /// Creates a page whose cursor can be created directly from the page item.
    /// </summary>
    public static Page<T> Create(
        ImmutableArray<T> items,
        bool hasNextPage,
        bool hasPreviousPage,
        Func<T, string> createCursor,
        int? totalCount = null)
        => new ValueCursorPage<T>(items, hasNextPage, hasPreviousPage, createCursor, totalCount);

    /// <summary>
    /// Creates a page whose cursor can be created directly from the page item.
    /// </summary>
    public static Page<T> Create(
        ImmutableArray<T> items,
        bool hasNextPage,
        bool hasPreviousPage,
        Func<T, int, int, int, string> createCursor,
        int index,
        int requestedPageSize,
        int totalCount)
        => new ValueCursorPage<T>(
            items,
            hasNextPage,
            hasPreviousPage,
            createCursor,
            index,
            requestedPageSize,
            totalCount);

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
            items,
            elements,
            hasNextPage,
            hasPreviousPage,
            createCursor,
            totalCount);

    /// <summary>
    /// Creates a page whose cursor must be created from a different source element than the page item.
    /// </summary>
    public static Page<T> Create<TElement>(
        ImmutableArray<T> items,
        ImmutableArray<TElement> elements,
        bool hasNextPage,
        bool hasPreviousPage,
        Func<TElement, int, int, int, string> createCursor,
        int index,
        int requestedPageSize,
        int totalCount)
        => new ElementCursorPage<TElement, T>(
            items,
            elements,
            hasNextPage,
            hasPreviousPage,
            createCursor,
            index,
            requestedPageSize,
            totalCount);
}
