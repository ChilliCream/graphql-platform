using System.Diagnostics;
using static HotChocolate.Buffers.FixedSizeArrayPoolEventSource;
using static HotChocolate.Buffers.Properties.BuffersResources;

namespace HotChocolate.Buffers;

internal sealed class FixedSizeArrayPool : IDisposable
{
    private readonly int _poolId;
    private readonly int _arraySize;
    private readonly int _numberOfArrays;
    private readonly Bucket _bucket;

    public FixedSizeArrayPool(
        int poolId,
        int arraySize,
        int[] levels,
        TimeSpan trimInterval,
        bool preAllocate)
    {
        Debug.Assert(
            levels.Length > 0,
            "Levels must be a non-empty array.");
        Debug.Assert(
            trimInterval.TotalSeconds > 10,
            "Trim interval should be greater than 10 seconds to avoid excessive trimming.");

        _poolId = poolId;
        _arraySize = arraySize;
        _numberOfArrays = levels[levels.Length - 1];
        _bucket = new Bucket(poolId, arraySize, levels, trimInterval, preAllocate);
        Log.PoolCreated(poolId, _numberOfArrays, (long)_numberOfArrays * arraySize);
    }

    public byte[] Rent()
    {
        var log = Log;
        var buffer = _bucket.Rent();

        if (buffer == null)
        {
            buffer = new byte[_arraySize];

            if (log.IsEnabled())
            {
                log.PoolExhausted(_poolId, _numberOfArrays);
            }
        }

        if (log.IsEnabled())
        {
            log.BufferRented(buffer.GetHashCode(), buffer.Length, _poolId, _bucket.InUse);
        }

        return buffer;
    }

    public void Return(byte[] array)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(array);
#else
        if (array is null)
        {
            throw new ArgumentNullException(nameof(array));
        }
#endif

        if (array.Length != _arraySize)
        {
            throw new ArgumentException(
                string.Format(FixedSizeArrayPool_Return_InvalidArraySize, array.Length, _arraySize),
                nameof(array));
        }

        var log = Log;
        var returned = _bucket.Return(array);

        if (log.IsEnabled())
        {
            log.BufferReturned(array.GetHashCode(), array.Length, _poolId, _bucket.InUse);
        }

        if (!returned)
        {
            if (log.IsEnabled())
            {
                log.BufferDropped(array.GetHashCode(), array.Length, _poolId);
            }
        }
    }

    public void Abandon(int count) => _bucket.Abandon(count);

    /// <summary>
    /// Gets the number of array references the pool has reserved slots for.
    /// </summary>
    internal int Capacity => _bucket.Capacity;

    internal void Trim() => _bucket.Trim();

    public void Dispose() => _bucket.Dispose();

    private sealed class Bucket : IDisposable
    {
        private const int PageShift = 12;
        private const int PageSize = 1 << PageShift;
        private const int PageMask = PageSize - 1;
        private readonly int _poolId;
        private readonly int _bufferLength;
        private readonly byte[]?[]?[] _pages;
        private readonly int[] _levels;
        private readonly Timer _trimTimer;
        private int _currentLevel;
        private int _capacity;
        private int _inUse;
        private SpinLock _lock;
        private int _index;

        internal Bucket(
            int poolId,
            int bufferLength,
            int[] levels,
            TimeSpan trimInterval,
            bool preAllocate)
        {
            var stableLevel = levels[0];

            _poolId = poolId;
            _bufferLength = bufferLength;
            _levels = levels;
            _currentLevel = 0;

            // the slots are kept in pages that are created as the pool grows into them,
            // only the slots of the stable level (first entry in _levels) exist up front.
            _pages = new byte[]?[]?[((levels[levels.Length - 1] - 1) >> PageShift) + 1];

            for (var i = 0; i < stableLevel; i += PageSize)
            {
                var page = new byte[Math.Min(PageSize, stableLevel - i)][];
                _pages[i >> PageShift] = page;
                _capacity += page.Length;
            }

            if (preAllocate)
            {
                // only pre-allocate up to the stable level.
                for (var i = 0; i < stableLevel; i++)
                {
                    _pages[i >> PageShift]![i & PageMask] = new byte[_bufferLength];
                }
            }

            _lock = new SpinLock(Debugger.IsAttached);
            _index = 0;
            _inUse = 0;

#pragma warning disable IDE0370 // Remove unnecessary suppression -- Required for netstandard2.0.
            _trimTimer = new Timer(static b => ((Bucket)b!).Trim(), this, trimInterval, trimInterval);
#pragma warning restore IDE0370 // Remove unnecessary suppression
        }

        internal int InUse => _inUse;

        internal int Capacity => _capacity;

        internal byte[]? Rent()
        {
            Interlocked.Increment(ref _inUse);

            byte[]? buffer = null;

            // While holding the lock, grab whatever is at the next available index and
            // update the index.  We do as little work as possible while holding the spin
            // lock to minimize contention with other threads. The try/finally is
            // necessary to properly handle thread aborts on platforms which have them.
            var lockTaken = false;
            var allocateBuffer = false;

            try
            {
                _lock.Enter(ref lockTaken);

                var limit = _levels[_currentLevel];

                if (_index >= limit && _currentLevel < _levels.Length - 1)
                {
                    limit = _levels[++_currentLevel];
                }

                if (_index < limit)
                {
                    var pageIndex = _index >> PageShift;
                    var offset = _index & PageMask;
                    var page = _pages[pageIndex];

                    if (page is null || offset >= page.Length)
                    {
                        // the pool grew past the slots of this page. Every slot below the index
                        // is rented out and therefore empty, so the longer page starts empty.
                        var previousLength = page?.Length ?? 0;
                        page = new byte[Math.Min(PageSize, limit - (pageIndex << PageShift))][];
                        _pages[pageIndex] = page;
                        _capacity += page.Length - previousLength;
                    }

                    buffer = page[offset];
                    page[offset] = null;
                    _index++;
                    allocateBuffer = buffer == null;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }

            // While we were holding the lock, we grabbed whatever was at the next available index, if
            // there was one. If we tried and if we got back null, that means we hadn't yet allocated
            // for that slot, in which case we should do so now.
            if (allocateBuffer)
            {
                buffer = new byte[_bufferLength];

                var log = Log;
                if (log.IsEnabled())
                {
                    log.BufferAllocated(buffer.GetHashCode(), _bufferLength, _poolId);
                }
            }

            return buffer;
        }

        internal bool Return(byte[] array)
        {
            Interlocked.Decrement(ref _inUse);

            // if the returned array has not the expected size we will reject it without throwing an error.
            if (array.Length != _bufferLength)
            {
                return false;
            }

            var returned = false;
            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);

                if (_index > 0)
                {
                    var index = --_index;
                    _pages[index >> PageShift]![index & PageMask] = array;
                    returned = true;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }

            return returned;
        }

        // decrements the outstanding count without storing the arrays for reuse.
        internal void Abandon(int count) => Interlocked.Add(ref _inUse, -count);

        // called from the TrimCallback timer once per minute.
        // if the pool is not under pressure we step the current level down one
        // notch and null out the buffer slots between the new and old level so
        // those byte[] arrays become eligible for collection.
        internal void Trim()
        {
            // nothing to trim if we are already at the stable level.
            if (_currentLevel == 0)
            {
                return;
            }

            int previousLimit;
            int inUse;
            var trimmed = 0;
            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);

                if (_currentLevel == 0)
                {
                    return;
                }

                previousLimit = _levels[_currentLevel - 1];
                inUse = _inUse;

                // if outstanding buffers exceed the target level the pool is still
                // under pressure, skip trimming.
                if (inUse > previousLimit)
                {
                    return;
                }

                var currentLimit = _levels[_currentLevel];

                // null out slots between the new limit and the old limit.
                // slots that are rented out or were never reached are already empty.
                for (var i = previousLimit; i < currentLimit;)
                {
                    var offset = i & PageMask;
                    var count = Math.Min(currentLimit - i, PageSize - offset);
                    var page = _pages[i >> PageShift];

                    if (page is not null && offset < page.Length)
                    {
                        var slots = page.AsSpan(offset, Math.Min(count, page.Length - offset));

                        foreach (var buffer in slots)
                        {
                            if (buffer is not null)
                            {
                                trimmed++;
                            }
                        }

                        if (offset == 0)
                        {
                            // the whole page lies above the new limit, so its slots go as well.
                            _pages[i >> PageShift] = null;
                            _capacity -= page.Length;
                        }
                        else
                        {
                            slots.Clear();
                        }
                    }

                    i += count;
                }

                // if _index is beyond the new limit we need to pull it back
                // and release those buffers too.
                if (_index > previousLimit)
                {
                    _index = previousLimit;
                }

                _currentLevel--;
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }

            var log = Log;
            if (log.IsEnabled())
            {
                log.PoolTrimmed(_poolId, trimmed, previousLimit, inUse);
            }
        }

        public void Dispose() => _trimTimer.Dispose();
    }
}
