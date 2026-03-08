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
    private readonly Func<T, int, int, int, string> _createCursor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueCursorPage{T}"/> class.
    /// </summary>
    public ValueCursorPage(
        ImmutableArray<T> items,
        bool hasNextPage,
        bool hasPreviousPage,
        Func<T, string> createCursor,
        int? totalCount = null)
        : base(items, hasNextPage, hasPreviousPage, totalCount)
    {
        _createCursor = (item, _, _, _) => createCursor(item);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueCursorPage{T}"/> class.
    /// </summary>
    public ValueCursorPage(
        ImmutableArray<T> items,
        bool hasNextPage,
        bool hasPreviousPage,
        Func<T, int, int, int, string> createCursor,
        int index,
        int requestedPageSize,
        int totalCount)
        : base(items, hasNextPage, hasPreviousPage, index, requestedPageSize, totalCount)
    {
        _createCursor = createCursor;
    }

    /// <summary>
    /// An empty page.
    /// </summary>
    public static new ValueCursorPage<T> Empty { get; } = new([], false, false, _ => string.Empty);

    protected override string CreateCursor(T item, int offset, int pageIndex, int totalCount)
        => _createCursor(item, offset, pageIndex, totalCount);
}
