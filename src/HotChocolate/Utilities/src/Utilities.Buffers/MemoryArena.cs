using static HotChocolate.Buffers.MemoryArenaEventSource;

namespace HotChocolate.Buffers;

/// <summary>
/// A scope-bound bump allocator that carves <see cref="MemorySegment"/> slabs out of pages rented
/// from <see cref="JsonMemory"/>. Pages are released in a single operation when the arena is disposed.
/// </summary>
/// <remarks>
/// <para>
/// An arena is bound to a single execution scope, for example a request or a single subscription
/// event. <see cref="Rent(int)"/> is thread-safe while the arena is open, and the memory it hands out
/// stays valid until the arena is disposed.
/// </para>
/// <para>
/// The lifecycle is open, then sealed, then disposed. <see cref="Seal"/> marks the transition from
/// the writer phase to a stable reader phase: no further rentals are accepted, but the memory
/// already handed out stays valid. <see cref="Dispose()"/> after <see cref="Seal"/> returns the pages
/// to the pool for reuse. <see cref="Dispose()"/> without a prior <see cref="Seal"/> intentionally
/// abandons the pages instead of returning them, so abandoned work that may still read or write
/// them cannot collide with another scope that reuses a pooled array. An arena must not be used
/// after it has been disposed.
/// </para>
/// </remarks>
internal sealed class MemoryArena : IMemoryArena, IDisposable
{
    // The cursor packs (pageIndex, offset) into a single word so the pair advances atomically:
    // the high 32 bits are the current page index, the low 32 bits the bump offset into that page.
    // A page index of -1 means no page has been rented yet.
    private const long InitialState = -1L << 32;

    // The arena advances through these phases. Open accepts rentals; sealed stops new rentals but
    // keeps handed-out memory valid; disposed has released its pages. The phase only moves forward.
    private const int Open = 0;
    private const int Sealed = 1;
    private const int Disposed = 2;

    // A process-wide counter assigns each arena a stable id so the lifecycle ledger can correlate
    // every rent, grow, seal and abandon back to the arena that performed it.
    private static long s_nextId;

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private MemorySegment[][]? _tables;
    private int _tableCount;
    private readonly long _id = Interlocked.Increment(ref s_nextId);
    private byte[][] _pages = new byte[4][];
    private long _state = InitialState;
    private int _pageCount;
    private int _phase;
    private long _rentCount;
    private long _rentBytes;

    /// <summary>
    /// Initializes a new instance of <see cref="MemoryArena"/>.
    /// </summary>
    public MemoryArena()
    {
        if (Log.IsEnabled())
        {
            Log.ArenaCreated(_id);
        }
    }

    /// <summary>
    /// Gets the number of pages this arena currently holds.
    /// </summary>
    internal int RentedPageCount => _pageCount;

    /// <summary>
    /// Gets the number of segment tables this arena currently tracks.
    /// </summary>
    internal int RentedTableCount
    {
        get
        {
            lock (_lock)
            {
                return _tableCount;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether this arena has been disposed.
    /// </summary>
    internal bool IsDisposed => Volatile.Read(ref _phase) == Disposed;

    /// <summary>
    /// Gets a value indicating whether this arena has been sealed.
    /// </summary>
    internal bool IsSealed => Volatile.Read(ref _phase) == Sealed;

    /// <summary>
    /// Rents a slab of <paramref name="size"/> bytes carved from the arena's current page.
    /// </summary>
    /// <remarks>
    /// This method is safe to call concurrently while the arena is open. The returned memory
    /// remains valid until the arena is disposed. Calling it after the arena has been sealed or
    /// disposed throws.
    /// </remarks>
    /// <param name="size">The number of bytes to rent. Must not exceed the page size.</param>
    /// <returns>A segment of exactly <paramref name="size"/> bytes.</returns>
    public MemorySegment Rent(int size)
    {
        if (size < 0 || size > JsonMemory.BufferSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                size,
                $"The size must be between 0 and {JsonMemory.BufferSize}.");
        }

        var phase = Volatile.Read(ref _phase);

#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(phase == Disposed, this);
#else
        if (phase == Disposed)
        {
            throw new ObjectDisposedException(nameof(MemoryArena));
        }
#endif

        if (phase == Sealed)
        {
            throw new InvalidOperationException(
                "The memory arena has been sealed and no longer accepts rentals.");
        }

        while (true)
        {
            var state = Interlocked.Read(ref _state);
            var pageIndex = (int)(state >> 32);
            var offset = (int)state;

            if (pageIndex >= 0 && offset + size <= JsonMemory.BufferSize)
            {
                // fast path: claim [offset, offset + size) by advancing the cursor atomically.
                var next = ((long)pageIndex << 32) | (uint)(offset + size);

                if (Interlocked.CompareExchange(ref _state, next, state) == state)
                {
                    // Re-read the pages after winning the claim: a concurrent dispose empties the
                    // array, in which case the rental must fail as disposed instead of faulting on
                    // the cleared page table.
                    var pages = Volatile.Read(ref _pages);

#if NET8_0_OR_GREATER
                    ObjectDisposedException.ThrowIf(pageIndex >= pages.Length, this);
#else
                    if (pageIndex >= pages.Length)
                    {
                        throw new ObjectDisposedException(nameof(MemoryArena));
                    }
#endif

                    // Count this bump rental for the seal-time summary. The plain static flag keeps
                    // the disabled path to a single field read and a not-taken branch; the counters
                    // are interlocked because concurrent threads bump the same arena.
                    if (IsTracingEnabled)
                    {
                        Interlocked.Increment(ref _rentCount);
                        Interlocked.Add(ref _rentBytes, size);
                    }

                    return new MemorySegment(pages[pageIndex], offset, size);
                }

                // another thread advanced the cursor first; retry.
                continue;
            }

            // slow path: the current page is full (or none exists yet); roll to a new page.
            RollOver(pageIndex);
        }
    }

    private void RollOver(int observedPageIndex)
    {
        // Rent the page outside the lock: allocation must never happen while the lock is held.
        var page = JsonMemory.Rent(JsonMemoryKind.Arena);
        var published = false;

        lock (_lock)
        {
            // Only roll if no other thread has already advanced past the page we observed.
            // The page index changes only here under the lock, so this comparison is stable.
            if (Volatile.Read(ref _phase) != Disposed
                && (int)(Interlocked.Read(ref _state) >> 32) == observedPageIndex)
            {
                var newIndex = observedPageIndex + 1;

                if (newIndex >= _pages.Length)
                {
                    var grown = new byte[_pages.Length * 2][];
                    Array.Copy(_pages, grown, _pageCount);
                    Volatile.Write(ref _pages, grown);
                }

                _pages[newIndex] = page;
                _pageCount = newIndex + 1;
                Interlocked.Exchange(ref _state, (long)newIndex << 32);
                published = true;

                if (Log.IsEnabled())
                {
                    Log.MemoryRented(_id, JsonMemory.BufferSize);
                }
            }
        }

        if (!published)
        {
            // We lost the rollover race or the arena was disposed; hand the page back.
            JsonMemory.Return(JsonMemoryKind.Arena, page);
        }
    }

    /// <summary>
    /// Rents a <see cref="MemorySegment"/> table of at least <paramref name="minLength"/> entries.
    /// The table's lifetime is bound to the arena: the arena reclaims it when it is disposed, so the
    /// caller must not return the table itself.
    /// </summary>
    /// <param name="minLength">The minimum number of entries the table must hold.</param>
    /// <returns>A table of at least <paramref name="minLength"/> entries.</returns>
    public MemorySegment[] RentSegmentTable(int minLength)
    {
        var table = MemorySegmentTablePool.Rent(minLength);

        lock (_lock)
        {
            // Rent the tracker on the first table. Each arena keeps one tracker for its lifetime and
            // returns it on seal, so tracking adds no per-request allocation once the pool is warm.
            _tables ??= MemorySegmentTableTrackerPool.Rent();

            if (_tableCount == _tables.Length)
            {
                GrowTracker();
            }

            _tables[_tableCount++] = table;
        }

        if (Log.IsEnabled())
        {
            Log.ArrayRented(_id, table.Length);
        }

        return table;
    }

    // Grows the tracker when a request rents more tables than the current tracker holds. The old
    // tracker is returned once its entries are copied (the pool parks a pooled-capacity one and
    // drops an already-grown one), so growing past a pooled tracker does not drain the pool. The
    // grown array is a plain allocation that the pool drops on its next return. Rare cold path,
    // called under the arena lock.
    private void GrowTracker()
    {
        var old = _tables!;
        var grown = new MemorySegment[old.Length * 2][];
        Array.Copy(old, grown, _tableCount);
        _tables = grown;

        MemorySegmentTableTrackerPool.Return(old, _tableCount);
    }

    /// <summary>
    /// Grows the given <see cref="MemorySegment"/> table to twice its current length, copying the
    /// existing entries into the new table. The new table's lifetime is bound to the arena just like
    /// the original, so the caller must not return either table itself.
    /// </summary>
    /// <param name="table">The table to grow. On return it references the larger table.</param>
    public void GrowSegmentTable(ref MemorySegment[] table)
    {
        var grown = MemorySegmentTablePool.Rent(table.Length * 2);
        Array.Copy(table, grown, table.Length);

        var old = table;

        lock (_lock)
        {
            // Replace the tracked table in place: the old array is no longer tracked, the new one is,
            // so a later dispose does not return either array twice.
            var tables = _tables;
            for (var i = 0; i < _tableCount; i++)
            {
                if (ReferenceEquals(tables![i], old))
                {
                    tables[i] = grown;
                    break;
                }
            }
        }

        // The old array is returned now: its entries have been copied into the grown table and no
        // tracking still references it, so there is no double-return on dispose.
        MemorySegmentTablePool.Return(old);

        if (Log.IsEnabled())
        {
            Log.ArrayGrown(_id, old.Length, grown.Length);
        }

        table = grown;
    }

    /// <summary>
    /// Seals the arena so that no further <see cref="Rent(int)"/> calls are accepted. The memory already
    /// handed out stays valid. Sealing marks the transition from the writer phase to a stable reader
    /// phase and is what makes a later <see cref="Dispose()"/> safe to return pages to the pool.
    /// Sealing is idempotent.
    /// </summary>
    public void Seal() => Interlocked.CompareExchange(ref _phase, Sealed, Open);

    /// <summary>
    /// Releases the arena's pages. If the arena was sealed the pages are returned to
    /// <see cref="JsonMemory"/> for reuse. If it was not sealed the pages are abandoned instead of
    /// returned, so abandoned work that may still hold a <see cref="MemorySegment"/> into them
    /// cannot collide with another scope that reuses a pooled array.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(disposing: true);
    }

    private void Dispose(bool disposing)
    {
        // Move to the disposed phase exactly once. The previous phase tells us whether the arena
        // was sealed. A sealed arena returns its pages to the pool, but only on the deterministic
        // dispose path. The finalizer is a leak backstop and always abandons, because it cannot
        // prove that no handed-out segment still references a page.
        var previous = Interlocked.Exchange(ref _phase, Disposed);

        if (previous == Disposed)
        {
            return;
        }

        var reclaim = disposing && (previous == Sealed);

        byte[][] pages;
        int count;
        MemorySegment[][]? tables;
        int tableCount;

        lock (_lock)
        {
            pages = _pages;
            count = _pageCount;

            // Detach the tracker before clearing tracking so the reclaim path can return the tables
            // and the tracker outside the lock. The abandon path drops both on the floor for the GC.
            tables = _tables;
            tableCount = _tableCount;
            _tables = null;
            _tableCount = 0;

            // Publish an empty page table with a volatile write so the lock-free reader in Rent
            // observes it and fails as disposed instead of reading freed state.
            Volatile.Write(ref _pages, []);
            _pageCount = 0;
            _state = InitialState;
        }

        if (reclaim)
        {
            if (count > 0)
            {
                JsonMemory.Return(JsonMemoryKind.Arena, pages, count);
            }

            // The arena owns the tables, so on a sealed deterministic dispose it returns each one to
            // the pool and then returns the tracker itself. The abandon path below intentionally
            // skips this, because a live document may still index into a table.
            if (tables is not null)
            {
                for (var i = 0; i < tableCount; i++)
                {
                    MemorySegmentTablePool.Return(tables[i]);
                }

                MemorySegmentTableTrackerPool.Return(tables, tableCount);
            }

            if (Log.IsEnabled())
            {
                Log.ArenaSealed(
                    _id,
                    count,
                    (long)count * JsonMemory.BufferSize,
                    tableCount,
                    Interlocked.Read(ref _rentCount),
                    Interlocked.Read(ref _rentBytes));
            }

            return;
        }

        // The abandon event is emitted before the count check so that tables rented without a page
        // are still recorded as abandoned.
        if (Log.IsEnabled())
        {
            Log.ArenaAbandoned(
                _id,
                count,
                (long)count * JsonMemory.BufferSize,
                tableCount,
                disposing ? 0 : 1);
        }

        if (count == 0)
        {
            return;
        }

        JsonMemory.Abandon(JsonMemoryKind.Arena, count);
    }

    ~MemoryArena() => Dispose(disposing: false);
}
