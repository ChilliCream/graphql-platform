using System.Collections;
using System.Collections.Immutable;

namespace GreenDonut.Data;

/// <summary>
/// Represents a page of a result set.
/// </summary>
/// <typeparam name="T">
/// The type of the items.
/// </typeparam>
public sealed class Page<T> : IEnumerable<T>
{
    private readonly ImmutableArray<T> _items;
    private readonly bool _hasNextPage;
    private readonly bool _hasPreviousPage;
    private readonly Func<T, int, int, int, string> _createCursor;
    private readonly int? _totalCount;
    private readonly int? _index;

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
    /// <param name="createCursor">
    /// A delegate to create a cursor for an item.
    /// </param>
    /// <param name="totalCount">
    /// The total count of items in the dataset.
    /// </param>
    public Page(
        ImmutableArray<T> items,
        bool hasNextPage,
        bool hasPreviousPage,
        Func<T, string> createCursor,
        int? totalCount = null)
    {
        _items = items;
        _hasNextPage = hasNextPage;
        _hasPreviousPage = hasPreviousPage;
        _createCursor = (item, _, _, _) =>  createCursor(item);
        _totalCount = totalCount;
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
    /// <param name="createCursor">
    /// A delegate to create a cursor for an item.
    /// </param>
    /// <param name="totalCount">
    /// The total count of items in the dataset.
    /// </param>
    /// <param name="index">
    /// The index number of this page.
    ///</param>
    internal Page(
        ImmutableArray<T> items,
        bool hasNextPage,
        bool hasPreviousPage,
        Func<T, int, int, int, string> createCursor,
        int index,
        int totalCount)
    {
        _items = items;
        _hasNextPage = hasNextPage;
        _hasPreviousPage = hasPreviousPage;
        _createCursor = createCursor;
        _totalCount = totalCount;
        _index = index;
    }

    /// <summary>
    /// Gets the items of this page.
    /// </summary>
    public ImmutableArray<T> Items => _items;

    /// <summary>
    /// Gets the first item of this page.
    /// </summary>
    public T? First => _items.Length > 0 ? _items[0] : default;

    /// <summary>
    /// Gets the last item of this page.
    /// </summary>
    public T? Last => _items.Length > 0 ? _items[^1] : default;

    /// <summary>
    /// Defines if there is a next page.
    /// </summary>
    public bool HasNextPage => _hasNextPage;

    /// <summary>
    /// Defines if there is a previous page.
    /// </summary>
    public bool HasPreviousPage => _hasPreviousPage;

    /// <summary>
    /// Gets the total count of items in the dataset.
    /// This value can be null if the total count is unknown.
    /// </summary>
    public int? TotalCount => _totalCount;

    /// <summary>
    /// Gets the index number of this page.
    /// </summary>
    public int? Index => _index;

    /// <summary>
    /// Creates a cursor for an item of this page.
    /// </summary>
    /// <param name="item">
    /// The item for which a cursor shall be created.
    /// </param>
    /// <returns>
    /// Returns a cursor for the item.
    /// </returns>
    public string CreateCursor(T item) => _createCursor(item, 0, 0, 0);

    public string CreateCursor(T item, int offset)
    {
        if(_index is null || _totalCount is null)
        {
            throw new InvalidOperationException("This page does not allow relative cursors.");
        }

        return _createCursor(item, offset, _index ?? 1, _totalCount ?? 0);
    }

    /// <summary>
    /// An empty page.
    /// </summary>
    public static Page<T> Empty => new(ImmutableArray<T>.Empty, false, false, _ => string.Empty);

    /// <summary>
    /// Gets the enumerator for the items of this page.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<T> GetEnumerator()
        => ((IEnumerable<T>)_items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
