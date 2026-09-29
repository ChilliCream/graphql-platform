namespace GreenDonut.Data.Internal;

/// <summary>
/// Owns the single shared source enumerator behind one or more streaming pages, and releases the
/// source and the lifetime once every page fed from it has completed or been disposed.
/// </summary>
/// <typeparam name="TElement">
/// The type of the source rows.
/// </typeparam>
internal sealed class StreamPagePump<TElement>
{
    private readonly IAsyncEnumerator<StreamRow<TElement>> _source;
    private StreamRow<TElement>? _primedFirstRow;
    private IAsyncDisposable? _lifetime;
    private int _livePages;
    private bool _sourceExhausted;
    private bool _sourceDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamPagePump{TElement}"/> class.
    /// </summary>
    /// <param name="source">
    /// The shared source enumerator that produces rows for the pages fed by this pump.
    /// </param>
    /// <param name="pageCount">
    /// The number of pages fed by this pump. The pump releases the source and the lifetime once
    /// this many pages have completed or been disposed.
    /// </param>
    /// <param name="lifetime">
    /// A resource owned by this pump, disposed once every page has completed or been disposed,
    /// or null if this pump owns nothing beyond the source.
    /// </param>
    /// <param name="primedFirstRow">
    /// A row already read from <paramref name="source"/> before construction, returned as the
    /// first result of <see cref="ReadNextAsync"/> without advancing the source again.
    /// </param>
    public StreamPagePump(
        IAsyncEnumerator<StreamRow<TElement>> source,
        int pageCount,
        IAsyncDisposable? lifetime = null,
        StreamRow<TElement>? primedFirstRow = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageCount, 1);

        _source = source;
        _livePages = pageCount;
        _lifetime = lifetime;
        _primedFirstRow = primedFirstRow;
    }

    /// <summary>
    /// Reads the next row from the shared source, or null once the source is exhausted.
    /// Reaching the end marks the source exhausted without disposing it; disposal happens when
    /// the pump is released.
    /// </summary>
    public async ValueTask<StreamRow<TElement>?> ReadNextAsync()
    {
        if (_primedFirstRow is { } row)
        {
            _primedFirstRow = null;
            return row;
        }

        if (_sourceExhausted)
        {
            return null;
        }

        if (await _source.MoveNextAsync().ConfigureAwait(false))
        {
            return _source.Current;
        }

        _sourceExhausted = true;
        return null;
    }

    /// <summary>
    /// Signals that one page fed by this pump has completed or been disposed; once every page has,
    /// disposes the source and then the lifetime exactly once. If both disposals throw, the
    /// source's exception is rethrown with the lifetime's attached.
    /// </summary>
    public async ValueTask ReleaseAsync()
    {
        if (--_livePages > 0)
        {
            return;
        }

        var lifetime = _lifetime;
        _lifetime = null;

        await OrderedDisposal.ReleaseAsync(
            DisposeSourceAsync,
            lifetime is null ? null : lifetime.DisposeAsync)
            .ConfigureAwait(false);
    }

    private async ValueTask DisposeSourceAsync()
    {
        if (_sourceDisposed)
        {
            return;
        }

        _sourceDisposed = true;
        await _source.DisposeAsync().ConfigureAwait(false);
    }
}
