using System.Collections.Immutable;

namespace GreenDonut.Data;

public interface IPage<T> : IEnumerable<T>
{
    /// <summary>
    /// Gets the items of this page.
    /// </summary>
    ImmutableArray<T> Items { get; }

    /// <summary>
    /// Gets the first item of this page.
    /// </summary>
    T? First { get; }

    /// <summary>
    /// Gets the last item of this page.
    /// </summary>
    T? Last { get; }

    /// <summary>
    /// Defines if there is a next page.
    /// </summary>
    bool HasNextPage { get; }

    /// <summary>
    /// Defines if there is a previous page.
    /// </summary>
    bool HasPreviousPage { get; }

    /// <summary>
    /// Gets the total count of items in the dataset.
    /// This value can be null if the total count is unknown.
    /// </summary>
    int? TotalCount { get; }

    /// <summary>
    /// Creates a cursor for an item of this page.
    /// </summary>
    /// <param name="item">
    /// The item for which a cursor shall be created.
    /// </param>
    /// <returns>
    /// Returns a cursor for the item.
    /// </returns>
    string CreateCursor(T item);
}
