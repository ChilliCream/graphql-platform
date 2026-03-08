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
    private readonly Func<TElement, int, int, int, string> _createCursor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElementCursorPage{TElement,TValue}"/> class.
    /// </summary>
    public ElementCursorPage(
        ImmutableArray<TValue> items,
        ImmutableArray<TElement> elements,
        bool hasNextPage,
        bool hasPreviousPage,
        Func<TElement, string> createCursor,
        int? totalCount = null)
        : base(items, hasNextPage, hasPreviousPage, totalCount)
    {
        EnsureEqualLength(items, elements);
        _elements = elements;
        _createCursor = (item, _, _, _) => createCursor(item);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ElementCursorPage{TElement,TValue}"/> class.
    /// </summary>
    public ElementCursorPage(
        ImmutableArray<TValue> items,
        ImmutableArray<TElement> elements,
        bool hasNextPage,
        bool hasPreviousPage,
        Func<TElement, int, int, int, string> createCursor,
        int index,
        int requestedPageSize,
        int totalCount)
        : base(items, hasNextPage, hasPreviousPage, index, requestedPageSize, totalCount)
    {
        EnsureEqualLength(items, elements);
        _elements = elements;
        _createCursor = createCursor;
    }

    protected override string CreateCursor(TValue item, int offset, int pageIndex, int totalCount)
        => _createCursor(GetElement(item), offset, pageIndex, totalCount);

    private TElement GetElement(TValue item)
    {
        for (var i = 0; i < Items.Length; i++)
        {
            if (ReferenceEquals(Items[i], item))
            {
                return _elements[i];
            }
        }

        var index = Items.IndexOf(item);

        if (index < 0)
        {
            throw new ArgumentException("The specified item does not belong to this page.", nameof(item));
        }

        return _elements[index];
    }

    private static void EnsureEqualLength(ImmutableArray<TValue> items, ImmutableArray<TElement> elements)
    {
        if (items.Length != elements.Length)
        {
            throw new ArgumentException("The items and elements collections must have the same length.");
        }
    }
}
