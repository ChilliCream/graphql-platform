using System.Collections;

namespace HotChocolate.Pagination;

/// <summary>
/// Represents a page of a result set.
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
/// <typeparam name="T">
/// The type of the items.
/// </typeparam>
public readonly struct Page<T>(
    IReadOnlyList<T> items,
    bool hasNextPage,
    bool hasPreviousPage,
    Func<T, string> createCursor)
    : IEnumerable<T>
{
    /// <summary>
    /// Gets the items of this page.
    /// </summary>
    public IReadOnlyList<T> Items => items;

    /// <summary>
    /// Gets the first item of this page.
    /// </summary>
    public T? First => items.Count > 0 ? items[0] : default;

    /// <summary>
    /// Gets the last item of this page.
    /// </summary>
    public T? Last => items.Count > 0 ? items[^1] : default;

    /// <summary>
    /// Defines if there is a next page.
    /// </summary>
    public bool HasNextPage => hasNextPage;

    /// <summary>
    /// Defines if there is a previous page.
    /// </summary>
    public bool HasPreviousPage => hasPreviousPage;

    /// <summary>
    /// Creates a cursor for an item of this page.
    /// </summary>
    /// <param name="item">
    /// The item for which a cursor shall be created.
    /// </param>
    /// <returns>
    /// Returns a cursor for the item.
    /// </returns>
    public string CreateCursor(T item) => createCursor(item);

    /// <summary>
    /// An empty page.
    /// </summary>
    public static Page<T> Empty => new(Array.Empty<T>(), false, false, _ => string.Empty);

    /// <summary>
    /// Gets the enumerator for the items of this page.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<T> GetEnumerator()
        => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
