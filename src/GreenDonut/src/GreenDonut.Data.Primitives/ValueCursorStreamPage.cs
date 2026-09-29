using GreenDonut.Data.Internal;

namespace GreenDonut.Data;

/// <summary>
/// Represents a streaming page whose cursor can be created directly from the page item.
/// </summary>
/// <typeparam name="T">
/// The type of the page items.
/// </typeparam>
internal sealed class ValueCursorStreamPage<T> : StreamPage<T>
{
    private readonly StreamPageBuffer<T> _buffer;
    private readonly Func<EdgeEntry<T>, string> _createCursor;

    private ValueCursorStreamPage(
        StreamPagePump<T>? pump,
        StreamPageDefinition<T> definition,
        Func<EdgeEntry<T>, string> createCursor)
        : this(new StreamPageBuffer<T>(pump, definition), definition.Index, createCursor)
    {
    }

    private ValueCursorStreamPage(
        StreamPageBuffer<T> buffer,
        int? index,
        Func<EdgeEntry<T>, string> createCursor)
        : base(buffer, index)
    {
        _buffer = buffer;
        _createCursor = createCursor;
    }

    /// <summary>
    /// An empty page.
    /// </summary>
    public static new ValueCursorStreamPage<T> Empty { get; } =
        new(
            pump: null,
            definition: new StreamPageDefinition<T>(
                RequestedCount: 0,
                Forward: false,
                TrailingSentinel: false,
                SkipFront: 0,
                SkipFrontFromCount: null,
                Index: null,
                RequestedSize: null,
                TotalCount: 0,
                HasNextPage: false,
                HasPreviousPage: false,
                FlagsFromFirstRow: null),
            createCursor: static _ => string.Empty);

    /// <summary>
    /// Creates a page whose buffer is already primed.
    /// </summary>
    /// <param name="pump">
    /// The pump this page reads from, or null for an already fully resolved page.
    /// </param>
    /// <param name="definition">
    /// The definition that governs how rows turn into content, flags, and a total count.
    /// </param>
    /// <param name="createCursor">
    /// Creates a cursor from a page item.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel priming the page.
    /// </param>
    internal static async ValueTask<StreamPage<T>> CreatePrimedAsync(
        StreamPagePump<T>? pump,
        StreamPageDefinition<T> definition,
        Func<EdgeEntry<T>, string> createCursor,
        CancellationToken cancellationToken = default)
    {
        var buffer = await StreamPageBuffer<T>.CreatePrimedAsync(
            pump,
            definition,
            cancellationToken)
            .ConfigureAwait(false);

        return new ValueCursorStreamPage<T>(buffer, definition.Index, createCursor);
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
    /// <param name="createCursor">
    /// Creates a cursor from a page item.
    /// </param>
    internal static StreamPage<T> CreateForBatch(
        StreamPagePump<T> pump,
        StreamPageDefinition<T> definition,
        Func<EdgeEntry<T>, string> createCursor)
        => new ValueCursorStreamPage<T>(pump, definition, createCursor);

    protected override string CreateCursor(int index, int offset, int pageIndex, int totalCount)
        => _createCursor(new EdgeEntry<T>(_buffer[index], offset, pageIndex, totalCount));
}
