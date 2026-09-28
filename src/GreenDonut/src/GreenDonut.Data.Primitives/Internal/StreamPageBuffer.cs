using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace GreenDonut.Data.Internal;

/// <summary>
/// Buffers the rows of a streaming page in a grow-only list, replays them for every enumerator,
/// and resolves the page's flags, total count, and completion from the shared source described
/// by a <see cref="StreamPageDefinition{TElement}"/>.
/// </summary>
/// <typeparam name="TElement">
/// The type of the buffered source rows.
/// </typeparam>
/// <remarks>
/// Enumerators read the shared buffer and advance the source when they reach its end; several
/// enumerators may interleave, but this type has no cross-thread safety, the same stance as
/// <c>DbContext</c>.
/// </remarks>
internal sealed class StreamPageBuffer<TElement> : IStreamPageSource<TElement>
{
    private readonly List<TElement> _items = [];
    private readonly StreamPagePump<TElement>? _pump;
    private readonly StreamPageDefinition<TElement> _definition;
    private int _skipRemaining;
    private bool _skipFromCountApplied;
    private bool _firstRowObserved;
    private bool _isCompleted;
    private int? _totalCount;
    private bool? _hasNextPage;
    private bool? _hasPreviousPage;
    private ExceptionDispatchInfo? _fault;
    private bool _pumpReleased;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamPageBuffer{TElement}"/> class.
    /// </summary>
    /// <param name="pump">
    /// The pump this buffer reads from, or null for a page that is already fully resolved (an
    /// empty page, or a count-only page with nothing to stream).
    /// </param>
    /// <param name="definition">
    /// The definition that governs how rows turn into content, flags, and a total count.
    /// </param>
    public StreamPageBuffer(StreamPagePump<TElement>? pump, StreamPageDefinition<TElement> definition)
    {
        _pump = pump;
        _definition = definition;
        _skipRemaining = definition.SkipFront;
        _totalCount = definition.TotalCount;
        _hasNextPage = definition.HasNextPage;
        _hasPreviousPage = definition.HasPreviousPage;
        _isCompleted = pump is null;
    }

    /// <inheritdoc />
    public bool IsCompleted => _isCompleted;

    /// <inheritdoc />
    public int? TotalCount => _totalCount;

    /// <inheritdoc />
    public int? RequestedSize => _definition.RequestedSize;

    /// <inheritdoc />
    public int BufferedCount => _items.Count;

    /// <summary>
    /// Gets the buffered row at the given index, throwing when it has not streamed yet.
    /// </summary>
    public TElement this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_items.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _items[index];
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerator<TElement> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => EnumerateAsync(cancellationToken).GetAsyncEnumerator();

    /// <inheritdoc />
    public IAsyncEnumerable<PageEntry<TElement>> EnumerateEntriesAsync(CancellationToken cancellationToken = default)
        => EnumerateEntriesCore(cancellationToken);

    /// <inheritdoc />
    public async ValueTask<int?> TotalCountAsync(CancellationToken cancellationToken = default)
    {
        if (_totalCount is null && !_firstRowObserved && !_isCompleted)
        {
            await AdvanceAsync(cancellationToken).ConfigureAwait(false);
        }

        return _totalCount;
    }

    /// <inheritdoc />
    public async ValueTask<bool> HasNextPageAsync(CancellationToken cancellationToken = default)
    {
        while (_hasNextPage is null && !_isCompleted)
        {
            await AdvanceAsync(cancellationToken).ConfigureAwait(false);
        }

        return _hasNextPage ?? false;
    }

    /// <inheritdoc />
    public async ValueTask<bool> HasPreviousPageAsync(CancellationToken cancellationToken = default)
    {
        while (_hasPreviousPage is null && !_isCompleted)
        {
            await AdvanceAsync(cancellationToken).ConfigureAwait(false);
        }

        return _hasPreviousPage ?? false;
    }

    /// <inheritdoc />
    public ValueTask PrimeAsync(CancellationToken cancellationToken = default) => AdvanceAsync(cancellationToken);

    /// <inheritdoc />
    public PageEntry<TElement> GetBufferedEntry(int index) => new(this[index], index);

    /// <inheritdoc />
    public async ValueTask DrainAsync(CancellationToken cancellationToken = default)
    {
        while (!_isCompleted)
        {
            await AdvanceAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => CompleteAsync();

    private async IAsyncEnumerable<TElement> EnumerateAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var index = 0;

        while (true)
        {
            if (index < _items.Count)
            {
                yield return _items[index];
                index++;
                continue;
            }

            if (_isCompleted)
            {
                yield break;
            }

            await AdvanceAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async IAsyncEnumerable<PageEntry<TElement>> EnumerateEntriesCore(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var index = 0;

        while (true)
        {
            if (index < _items.Count)
            {
                yield return new PageEntry<TElement>(_items[index], index);
                index++;
                continue;
            }

            if (_isCompleted)
            {
                yield break;
            }

            await AdvanceAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    // Reads from the pump until either one more content row is buffered or the page completes
    // (the source is exhausted, or, for a forward page, the trailing sentinel is found). Rows
    // consumed by SkipFront are read and discarded in the same call.
    private async ValueTask AdvanceAsync(CancellationToken cancellationToken)
    {
        if (_isCompleted)
        {
            return;
        }

        _fault?.Throw();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            StreamRow<TElement>? row;

            try
            {
                row = _pump is null ? null : await _pump.ReadNextAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // a source that faults mid-stream releases the pump, exactly as reaching the end
                // of the source or disposing the page does, but the page itself stays not
                // completed so every later call rethrows the same exception instead of silently
                // truncating.
                _fault = ExceptionDispatchInfo.Capture(ex);
                await ReleasePumpAsync().ConfigureAwait(false);
                throw;
            }

            if (row is null)
            {
                await CompleteAsync().ConfigureAwait(false);
                return;
            }

            Observe(row);

            if (_skipRemaining > 0)
            {
                _skipRemaining--;
                continue;
            }

            if (_definition.Forward && _definition.TrailingSentinel && _items.Count == _definition.RequestedCount)
            {
                _hasNextPage = true;
                await CompleteAsync().ConfigureAwait(false);
                return;
            }

            _items.Add(row.Item);
            return;
        }
    }

    private void Observe(StreamRow<TElement> row)
    {
        if (_firstRowObserved)
        {
            return;
        }

        _firstRowObserved = true;
        _totalCount ??= row.TotalCount;

        if (_definition.SkipFrontFromCount is not null && _totalCount is not null && !_skipFromCountApplied)
        {
            _skipFromCountApplied = true;
            _skipRemaining = _definition.SkipFrontFromCount(_totalCount, _definition.SkipFront);
        }

        if (_definition.FlagsFromFirstRow is not null)
        {
            var (hasNext, hasPrevious) = _definition.FlagsFromFirstRow(row);
            _hasNextPage ??= hasNext;
            _hasPreviousPage ??= hasPrevious;
        }
    }

    private async ValueTask CompleteAsync()
    {
        if (_isCompleted || _fault is not null)
        {
            return;
        }

        _isCompleted = true;
        await ReleasePumpAsync().ConfigureAwait(false);
    }

    // Releases the pump exactly once, however release was triggered: normal completion or the
    // source faulting mid-stream. A faulted page stays not completed, so it needs its own
    // released flag separate from _isCompleted.
    private async ValueTask ReleasePumpAsync()
    {
        if (_pumpReleased)
        {
            return;
        }

        _pumpReleased = true;

        if (_pump is not null)
        {
            await _pump.ReleaseAsync().ConfigureAwait(false);
        }
    }
}
