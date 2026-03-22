using System.Collections.Immutable;

namespace GreenDonut.Data;

/// <summary>
/// Represents a page whose cursor can be created directly from the page item.
/// </summary>
/// <typeparam name="T">
/// The type of the page items.
/// </typeparam>
internal sealed class ValueCursorPage<T> : Page<T>
{
    private readonly Func<EdgeEntry<T>, string> _createCursor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueCursorPage{T}"/> class.
    /// </summary>
    public ValueCursorPage(
        ImmutableArray<PageEntry<T>> entries,
        bool hasNextPage,
        bool hasPreviousPage,
        Func<T, string> createCursor,
        int? totalCount = null)
        : base(entries, hasNextPage, hasPreviousPage, totalCount)
    {
        _createCursor = entry => createCursor(entry.Node);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueCursorPage{T}"/> class.
    /// </summary>
    public ValueCursorPage(
        ImmutableArray<PageEntry<T>> entries,
        bool hasNextPage,
        bool hasPreviousPage,
        Func<EdgeEntry<T>, string> createCursor,
        int index,
        int requestedPageSize,
        int totalCount)
        : base(entries, hasNextPage, hasPreviousPage, index, requestedPageSize, totalCount)
    {
        _createCursor = createCursor;
    }

    /// <summary>
    /// An empty page.
    /// </summary>
    public static new ValueCursorPage<T> Empty { get; } = new([], false, false, _ => string.Empty, 0);

    protected override string CreateCursor(int index, int offset, int pageIndex, int totalCount)
        => _createCursor(new EdgeEntry<T>(Entries[index].Item, offset, pageIndex, totalCount));
}
