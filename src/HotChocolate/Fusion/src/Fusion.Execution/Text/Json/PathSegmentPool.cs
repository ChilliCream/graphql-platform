using System.Diagnostics;
using static HotChocolate.Fusion.Text.Json.PathSegmentPoolEventSource;

namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// The shared pool of path segment arrays. Arrays are rented and returned in batches
/// and the pool is safe for concurrent use.
/// </summary>
internal sealed class PathSegmentPool : IDisposable
{
    private const int LockBatchSize = 32;
    private static int s_nextPoolId;
    internal readonly int _segmentArraySize;
    private readonly int _poolId;
    private readonly int _numberOfArrays;
    private readonly Bucket _bucket;

    public PathSegmentPool(int segmentArraySize, int[] levels, TimeSpan trimInterval, bool preAllocate)
    {
        Debug.Assert(segmentArraySize >= 32);
        Debug.Assert(
            levels.Length > 0,
            "Levels must be a non-empty array.");
        Debug.Assert(
            trimInterval.TotalSeconds > 10,
            "Trim interval should be greater than 10 seconds to avoid excessive trimming.");

        _segmentArraySize = segmentArraySize;
        _poolId = Interlocked.Increment(ref s_nextPoolId);
        _numberOfArrays = levels[levels.Length - 1];
        _bucket = new Bucket(_poolId, segmentArraySize, levels, trimInterval, preAllocate);

        var log = Log;
        if (log.IsEnabled())
        {
            log.PoolCreated(
                _poolId,
                _segmentArraySize,
                _numberOfArrays,
                (long)_numberOfArrays * _segmentArraySize * sizeof(int));
        }
    }

    /// <summary>
    /// Fills <paramref name="destination"/> with segment arrays.
    /// Slots the pool cannot serve receive newly allocated arrays.
    /// </summary>
    public void Rent(Span<int[]?> destination)
    {
        while (destination.Length > LockBatchSize)
        {
            RentBatch(destination[..LockBatchSize]);
            destination = destination[LockBatchSize..];
        }

        RentBatch(destination);
    }

    /// <summary>
    /// Returns segment arrays to the pool. Arrays the pool has no room for and arrays
    /// of another length are dropped. Null entries are ignored.
    /// </summary>
    public void Return(ReadOnlySpan<int[]?> arrays)
    {
        while (arrays.Length > LockBatchSize)
        {
            ReturnBatch(arrays[..LockBatchSize]);
            arrays = arrays[LockBatchSize..];
        }

        ReturnBatch(arrays);
    }

    private void RentBatch(Span<int[]?> destination)
    {
        var log = Log;
        var served = _bucket.Rent(destination);

        if (served < destination.Length)
        {
            for (var i = served; i < destination.Length; i++)
            {
                destination[i] = new int[_segmentArraySize];
            }

            if (log.IsEnabled())
            {
                log.PoolExhausted(_poolId, _numberOfArrays);
            }
        }

        if (log.IsEnabled())
        {
            var inUse = _bucket.InUse;

            foreach (var buffer in destination)
            {
                log.SegmentRented(buffer!.GetHashCode(), buffer.Length, _poolId, inUse);
            }
        }
    }

    private void ReturnBatch(ReadOnlySpan<int[]?> arrays)
    {
        var log = Log;
        var accepted = _bucket.Return(arrays);

        if (log.IsEnabled())
        {
            var inUse = _bucket.InUse;
            var matching = 0;

            for (var i = 0; i < arrays.Length; i++)
            {
                if (arrays[i] is not { } array)
                {
                    continue;
                }

                log.SegmentReturned(array.GetHashCode(), array.Length, _poolId, inUse);

                if (array.Length != _segmentArraySize || matching++ >= accepted)
                {
                    log.SegmentDropped(array.GetHashCode(), array.Length, _poolId);
                }
            }
        }
    }

    /// <summary>
    /// Gets the number of arrays rented from this pool that have not been returned.
    /// </summary>
    internal int InUse => _bucket.InUse;

    /// <summary>
    /// Gets the number of segment-array references reserved by the pool.
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
        private readonly int _segmentArraySize;
        private readonly int[]?[]?[] _pages;
        private readonly int[] _levels;
        private readonly Timer _trimTimer;
        private int _currentLevel;
        private int _capacity;
        private int _inUse;
        private SpinLock _lock;
        private int _index;

        internal Bucket(
            int poolId,
            int segmentArraySize,
            int[] levels,
            TimeSpan trimInterval,
            bool preAllocate)
        {
            _poolId = poolId;
            _segmentArraySize = segmentArraySize;
            _levels = levels;
            _pages = new int[]?[]?[((levels[^1] - 1) >> PageShift) + 1];

            for (var i = 0; i < levels[0]; i += PageSize)
            {
                var page = new int[Math.Min(PageSize, levels[0] - i)][];
                _pages[i >> PageShift] = page;
                _capacity += page.Length;
            }

            if (preAllocate)
            {
                for (var i = 0; i < levels[0]; i++)
                {
                    _pages[i >> PageShift]![i & PageMask] = new int[_segmentArraySize];
                }
            }

            _lock = new SpinLock(Debugger.IsAttached);
            _trimTimer = new Timer(static b => ((Bucket)b!).Trim(), this, trimInterval, trimInterval);
        }

        internal int InUse => _inUse;

        internal int Capacity => _capacity;

        /// <summary>
        /// Takes one pool slot per destination element until the pool is exhausted and
        /// returns the number of slots taken. Empty slots are filled with new arrays.
        /// </summary>
        internal int Rent(Span<int[]?> destination)
        {
            Interlocked.Add(ref _inUse, destination.Length);

            var served = 0;
            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);
                var limit = _levels[_currentLevel];

                while (served < destination.Length)
                {
                    if (_index >= limit && _currentLevel < _levels.Length - 1)
                    {
                        limit = _levels[++_currentLevel];
                    }

                    if (_index >= limit)
                    {
                        break;
                    }

                    var pageIndex = _index >> PageShift;
                    var page = _pages[pageIndex];
                    var offset = _index & PageMask;

                    if (page is null || offset >= page.Length)
                    {
                        var length = Math.Min(PageSize, limit - (pageIndex << PageShift));
                        var previousLength = page?.Length ?? 0;
                        page = new int[length][];
                        _pages[pageIndex] = page;
                        _capacity += length - previousLength;
                    }

                    var count = Math.Min(
                        destination.Length - served,
                        Math.Min(limit - _index, page.Length - offset));
                    var slots = page.AsSpan(offset, count);
                    slots.CopyTo(destination.Slice(served, count));
                    slots.Clear();
                    _index += count;
                    served += count;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }

            for (var i = 0; i < served; i++)
            {
                if (destination[i] is null)
                {
                    var buffer = new int[_segmentArraySize];
                    destination[i] = buffer;

                    var log = Log;
                    if (log.IsEnabled())
                    {
                        log.SegmentAllocated(buffer.GetHashCode(), _segmentArraySize, _poolId);
                    }
                }
            }

            return served;
        }

        /// <summary>
        /// Puts the arrays of the pool's segment size back into free pool slots and
        /// returns how many were accepted. Arrays of another length do not count as returned.
        /// </summary>
        internal int Return(ReadOnlySpan<int[]?> arrays)
        {
            var returned = 0;

            foreach (var array in arrays)
            {
                if (array is not null && array.Length == _segmentArraySize)
                {
                    returned++;
                }
            }

            Interlocked.Add(ref _inUse, -returned);

            var accepted = 0;
            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);
                var index = _index;
                var page = index == 0 ? null : _pages[(index - 1) >> PageShift]!;
                var offset = (index - 1) & PageMask;

                for (var i = 0; i < arrays.Length && index > 0; i++)
                {
                    var array = arrays[i];

                    if (array is null || array.Length != _segmentArraySize)
                    {
                        continue;
                    }

                    if (offset < 0)
                    {
                        page = _pages[(index - 1) >> PageShift]!;
                        offset = (index - 1) & PageMask;
                    }

                    page![offset--] = array;
                    index--;
                    accepted++;
                }

                _index = index;
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }

            return accepted;
        }

        internal void Trim()
        {
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

                if (inUse > previousLimit)
                {
                    return;
                }

                var currentLimit = _levels[_currentLevel];

                for (var i = previousLimit; i < currentLimit;)
                {
                    var offset = i & PageMask;
                    var count = Math.Min(currentLimit - i, PageSize - offset);
                    var page = _pages[i >> PageShift];

                    if (page is not null)
                    {
                        var slots = page.AsSpan(offset, count);

                        foreach (var array in slots)
                        {
                            if (array is not null)
                            {
                                trimmed++;
                            }
                        }

                        slots.Clear();
                    }

                    i += count;
                }

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
