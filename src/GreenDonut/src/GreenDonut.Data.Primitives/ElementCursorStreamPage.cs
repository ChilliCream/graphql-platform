using GreenDonut.Data.Internal;

namespace GreenDonut.Data;

/// <summary>
/// Represents a streaming page whose cursor must be created from a different source element than
/// the page item.
/// </summary>
/// <typeparam name="TElement">
/// The type of the source rows, used to create the cursor.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the page items.
/// </typeparam>
internal sealed class ElementCursorStreamPage<TElement, TValue> : StreamPage<TValue>
{
    private readonly StreamPageBuffer<TElement> _buffer;
    private readonly Func<EdgeEntry<TElement>, string> _createCursor;

    private ElementCursorStreamPage(
        StreamPagePump<TElement>? pump,
        StreamPageDefinition<TElement> definition,
        Func<TElement, TValue> valueSelector,
        Func<EdgeEntry<TElement>, string> createCursor)
        : this(new StreamPageBuffer<TElement>(pump, definition), definition.Index, valueSelector, createCursor)
    {
    }

    private ElementCursorStreamPage(
        StreamPageBuffer<TElement> buffer,
        int? index,
        Func<TElement, TValue> valueSelector,
        Func<EdgeEntry<TElement>, string> createCursor)
        : base(new ElementProjectingSource<TElement, TValue>(buffer, valueSelector), index)
    {
        _buffer = buffer;
        _createCursor = createCursor;
    }

    /// <summary>
    /// Creates a page whose buffer is already primed.
    /// </summary>
    /// <param name="pump">
    /// The pump this page reads from, or null for an already fully resolved page.
    /// </param>
    /// <param name="definition">
    /// The definition that governs how rows turn into content, flags, and a total count.
    /// </param>
    /// <param name="valueSelector">
    /// Projects a source row into a page item.
    /// </param>
    /// <param name="createCursor">
    /// Creates a cursor from a source row.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel priming the page.
    /// </param>
    internal static async ValueTask<StreamPage<TValue>> CreatePrimedAsync(
        StreamPagePump<TElement>? pump,
        StreamPageDefinition<TElement> definition,
        Func<TElement, TValue> valueSelector,
        Func<EdgeEntry<TElement>, string> createCursor,
        CancellationToken cancellationToken = default)
    {
        var buffer = await StreamPageBuffer<TElement>.CreatePrimedAsync(
            pump,
            definition,
            cancellationToken)
            .ConfigureAwait(false);

        return new ElementCursorStreamPage<TElement, TValue>(buffer, definition.Index, valueSelector, createCursor);
    }

    /// <summary>
    /// Creates an unprimed page for the batch pump's own per-key construction path.
    /// </summary>
    /// <param name="pump">
    /// The pump this page reads from.
    /// </param>
    /// <param name="definition">
    /// The definition that governs how rows turn into content, flags, and a total count.
    /// </param>
    /// <param name="valueSelector">
    /// Projects a source row into a page item.
    /// </param>
    /// <param name="createCursor">
    /// Creates a cursor from a source row.
    /// </param>
    internal static StreamPage<TValue> CreateForBatch(
        StreamPagePump<TElement> pump,
        StreamPageDefinition<TElement> definition,
        Func<TElement, TValue> valueSelector,
        Func<EdgeEntry<TElement>, string> createCursor)
        => new ElementCursorStreamPage<TElement, TValue>(pump, definition, valueSelector, createCursor);

    protected override string CreateCursor(int index, int offset, int pageIndex, int totalCount)
        => _createCursor(new EdgeEntry<TElement>(_buffer[index], offset, pageIndex, totalCount));
}
