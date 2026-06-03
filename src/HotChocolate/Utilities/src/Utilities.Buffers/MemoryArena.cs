using System.Diagnostics;

namespace HotChocolate.Buffers;

/// <summary>
/// A scope-bound bump allocator that carves <see cref="MemorySegment"/> slabs out of pages rented
/// from <see cref="JsonMemory"/>. Pages are released in a single operation when the arena is disposed.
/// </summary>
/// <remarks>
/// <para>
/// An arena is bound to a single execution scope, for example a request or a single subscription
/// event. <see cref="Rent"/> is thread-safe while the arena is open, and the memory it hands out
/// stays valid until the arena is disposed.
/// </para>
/// <para>
/// The lifecycle is open, then sealed, then disposed. <see cref="Seal"/> marks the transition from
/// the writer phase to a stable reader phase: no further rentals are accepted, but the memory
/// already handed out stays valid. <see cref="Dispose"/> after <see cref="Seal"/> returns the pages
/// to the pool for reuse. <see cref="Dispose"/> without a prior <see cref="Seal"/> intentionally
/// abandons the pages instead of returning them, so abandoned work that may still read or write
/// them cannot collide with another scope that reuses a pooled array. An arena must not be used
/// after it has been disposed.
/// </para>
/// </remarks>
internal sealed class MemoryArena : IDisposable
{
    // The cursor packs (pageIndex, offset) into a single word so the pair advances atomically:
    // the high 32 bits are the current page index, the low 32 bits the bump offset into that page.
    // A page index of -1 means no page has been rented yet.
    private const long InitialState = -1L << 32;

    private byte[][] _pages = new byte[4][];
    private long _state = InitialState;
    private SpinLock _lock = new(Debugger.IsAttached);
    private int _pageCount;
    private bool _disposed;
    private bool _sealed;

    /// <summary>
    /// Gets the number of pages this arena currently holds.
    /// </summary>
    internal int RentedPageCount => _pageCount;

    /// <summary>
    /// Gets a value indicating whether this arena has been disposed.
    /// </summary>
    internal bool IsDisposed => _disposed;

    /// <summary>
    /// Gets a value indicating whether this arena has been sealed.
    /// </summary>
    internal bool IsSealed => _sealed;

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

#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MemoryArena));
        }
#endif

        if (_sealed)
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
                    return new MemorySegment(Volatile.Read(ref _pages)[pageIndex], offset, size);
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
        // Rent the page outside the lock: allocation must never happen while a spin lock is held.
        var page = JsonMemory.Rent(JsonMemoryKind.Arena);
        var published = false;

        var lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);

            // Only roll if no other thread has already advanced past the page we observed.
            // The page index changes only here under the lock, so this comparison is stable.
            if (!_disposed && (int)(Interlocked.Read(ref _state) >> 32) == observedPageIndex)
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
        finally
        {
            if (lockTaken)
            {
                _lock.Exit(false);
            }
        }

        if (!published)
        {
            // We lost the rollover race or the arena was disposed; hand the page back.
            JsonMemory.Return(JsonMemoryKind.Arena, page);
        }
    }

    /// <summary>
    /// Seals the arena so that no further <see cref="Rent"/> calls are accepted. The memory already
    /// handed out stays valid. Sealing marks the transition from the writer phase to a stable reader
    /// phase and is what makes a later <see cref="Dispose"/> safe to return pages to the pool.
    /// Sealing is idempotent.
    /// </summary>
    public void Seal()
    {
        var lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);
            _sealed = true;
        }
        finally
        {
            if (lockTaken)
            {
                _lock.Exit(false);
            }
        }
    }

    /// <summary>
    /// Releases the arena's pages. If the arena was sealed the pages are returned to
    /// <see cref="JsonMemory"/> for reuse. If it was not sealed the pages are abandoned instead of
    /// returned, so abandoned work that may still hold a <see cref="MemorySegment"/> into them
    /// cannot collide with another scope that reuses a pooled array. Dispose is idempotent.
    /// </summary>
    public void Dispose()
    {
        byte[][] pages;
        int count;
        bool wasSealed;

        var lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);

            if (_disposed)
            {
                return;
            }

            _disposed = true;
            wasSealed = _sealed;
            pages = _pages;
            count = _pageCount;
            _pages = [];
            _pageCount = 0;
            _state = InitialState;
        }
        finally
        {
            if (lockTaken)
            {
                _lock.Exit(false);
            }
        }

        if (count == 0)
        {
            return;
        }

        // Done outside the lock; the pool has its own synchronization.
        if (wasSealed)
        {
            JsonMemory.Return(JsonMemoryKind.Arena, pages, count);
        }
        else
        {
            JsonMemory.Abandon(JsonMemoryKind.Arena, count);
        }
    }
}
