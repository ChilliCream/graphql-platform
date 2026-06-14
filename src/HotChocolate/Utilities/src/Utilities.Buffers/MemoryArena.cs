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

    // Bounded reuse pool for arena instances. Only the cheap machinery (the page table array and
    // the lock) is reused; the pages themselves go back to JsonMemory on dispose. The cap keeps the
    // retained instances bounded under burst load.
    private static readonly ArenaPool s_pool = new(Environment.ProcessorCount * 2);

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private byte[][] _pages = new byte[4][];
    private long _state = InitialState;
    private int _pageCount;
    private int _phase;

    /// <summary>
    /// Rents an open arena, reusing a pooled instance when one is available.
    /// </summary>
    internal static MemoryArena Rent()
    {
        var arena = s_pool.Rent();

        if (arena is null)
        {
            return new MemoryArena();
        }

        // The instance was handed back as a pooled, fully-disposed shell with its finalizer
        // suppressed. Re-arm it and reinstate the finalizer so an abandoned rental is still backed
        // by the leak backstop.
        GC.ReRegisterForFinalize(arena);
        arena.Reinitialize();
        return arena;
    }

    private void Reinitialize()
    {
        _state = InitialState;
        _pageCount = 0;
        _phase = Open;
    }

    /// <summary>
    /// Gets the number of pages this arena currently holds.
    /// </summary>
    internal int RentedPageCount => _pageCount;

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
            }
        }

        if (!published)
        {
            // We lost the rollover race or the arena was disposed; hand the page back.
            JsonMemory.Return(JsonMemoryKind.Arena, page);
        }
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

        var retainMemory = disposing && (previous == Sealed);

        byte[][] pages;
        int count;

        lock (_lock)
        {
            pages = _pages;
            count = _pageCount;

            // On the retain path the page table array is reused, so it is kept in place; the abandon
            // path publishes an empty table with a volatile write so the lock-free reader in Rent
            // observes it and fails as disposed instead of reading freed state.
            if (!retainMemory)
            {
                Volatile.Write(ref _pages, []);
            }

            _pageCount = 0;
            _state = InitialState;
        }

        if (retainMemory)
        {
            if (count > 0)
            {
                JsonMemory.Return(JsonMemoryKind.Arena, pages, count);
                // Clear the rented slots only after the pages have been handed back, so the reused
                // page table no longer pins them for the next rental.
                pages.AsSpan(0, count).Clear();
            }

            // The instance is a clean shell now; hand it back for reuse. The public Dispose already
            // suppressed its finalizer, so a pooled instance carries no pending backstop; Rent
            // re-registers it on the next rental.
            s_pool.Return(this);
            return;
        }

        if (count == 0)
        {
            return;
        }

        JsonMemory.Abandon(JsonMemoryKind.Arena, count);
    }

    ~MemoryArena() => Dispose(disposing: false);

    // A bounded, lock-free stack of reusable arena shells. Slots beyond the cap are dropped on
    // return so the retained set stays bounded, which keeps steady-state allocations at zero
    // without unbounded retention under burst load.
    private sealed class ArenaPool(int capacity)
    {
        private readonly MemoryArena?[] _slots = new MemoryArena[capacity];
        private int _count;

        public MemoryArena? Rent()
        {
            while (true)
            {
                var count = Volatile.Read(ref _count);

                if (count == 0)
                {
                    return null;
                }

                if (Interlocked.CompareExchange(ref _count, count - 1, count) == count)
                {
                    var arena = Interlocked.Exchange(ref _slots[count - 1], null);

                    if (arena is not null)
                    {
                        return arena;
                    }

                    // The slot was not yet published by a concurrent return; fall back to a fresh
                    // instance rather than spin.
                    return null;
                }
            }
        }

        public bool Return(MemoryArena arena)
        {
            while (true)
            {
                var count = Volatile.Read(ref _count);

                if (count >= _slots.Length)
                {
                    return false;
                }

                if (Interlocked.CompareExchange(ref _count, count + 1, count) == count)
                {
                    Volatile.Write(ref _slots[count], arena);
                    return true;
                }
            }
        }
    }
}
