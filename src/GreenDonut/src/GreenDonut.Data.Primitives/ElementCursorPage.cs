using System.Collections.Immutable;

namespace GreenDonut.Data;

/// <summary>
/// Represents a page whose cursor must be created from a different source element than the page item.
/// </summary>
/// <typeparam name="TElement">
/// The type used to create the cursor.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the page items.
/// </typeparam>
internal sealed class ElementCursorPage<TElement, TValue> : Page<TValue>
{
    private readonly ImmutableArray<TElement> _elements;
    private readonly Func<EdgeEntry<TElement>, string> _createCursor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElementCursorPage{TElement,TValue}"/> class.
    /// </summary>
    public ElementCursorPage(
        ImmutableArray<PageEntry<TValue>> entries,
        ImmutableArray<TElement> elements,
        bool hasNextPage,
        bool hasPreviousPage,
        Func<TElement, string> createCursor,
        int? totalCount = null,
        ImmutableArray<TValue> items = default)
        : base(entries, hasNextPage, hasPreviousPage, totalCount, items)
    {
        EnsureEqualLength(entries, elements);
        _elements = elements;
        _createCursor = entry => createCursor(entry.Node);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ElementCursorPage{TElement,TValue}"/> class.
    /// </summary>
    public ElementCursorPage(
        ImmutableArray<PageEntry<TValue>> entries,
        ImmutableArray<TElement> elements,
        bool hasNextPage,
        bool hasPreviousPage,
        Func<EdgeEntry<TElement>, string> createCursor,
        int index,
        int requestedPageSize,
        int totalCount,
        ImmutableArray<TValue> items = default)
        : base(entries, hasNextPage, hasPreviousPage, index, requestedPageSize, totalCount, items)
    {
        EnsureEqualLength(entries, elements);
        _elements = elements;
        _createCursor = createCursor;
    }

    protected override string CreateCursor(int index, int offset, int pageIndex, int totalCount)
        => _createCursor(new EdgeEntry<TElement>(_elements[index], offset, pageIndex, totalCount));

    private static void EnsureEqualLength(
        ImmutableArray<PageEntry<TValue>> entries,
        ImmutableArray<TElement> elements)
    {
        if (entries.Length != elements.Length)
        {
            throw new ArgumentException(
                "The items and elements collections must have the same length.");
        }
    }
}
