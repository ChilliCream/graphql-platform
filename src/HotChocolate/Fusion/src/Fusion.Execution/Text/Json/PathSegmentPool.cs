using System.Diagnostics;
using static HotChocolate.Fusion.Text.Json.PathSegmentPoolEventSource;

namespace HotChocolate.Fusion.Text.Json;

internal sealed class PathSegmentPool : IDisposable
{
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

    public int[] Rent()
    {
        var log = Log;
        var buffer = _bucket.Rent();

        if (buffer is null)
        {
            buffer = new int[_segmentArraySize];

            if (log.IsEnabled())
            {
                log.PoolExhausted(_poolId, _numberOfArrays);
            }
        }

        if (log.IsEnabled())
        {
            log.SegmentRented(buffer.GetHashCode(), buffer.Length, _poolId, _bucket.InUse);
        }

        return buffer;
    }

    public void Return(int[] array)
    {
        if (array.Length != _segmentArraySize)
        {
            return;
        }

        var log = Log;
        var returned = _bucket.Return(array);

        if (log.IsEnabled())
        {
            log.SegmentReturned(array.GetHashCode(), array.Length, _poolId, _bucket.InUse);
        }

        if (!returned && log.IsEnabled())
        {
            log.SegmentDropped(array.GetHashCode(), array.Length, _poolId);
        }
    }

    public void Dispose() => _bucket.Dispose();

    private sealed class Bucket : IDisposable
    {
        private readonly int _poolId;
        private readonly int _segmentArraySize;
        private readonly int[]?[] _buffers;
        private readonly int[] _levels;
        private readonly Timer _trimTimer;
        private int _currentLevel;
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
            var numberOfBuffers = levels[levels.Length - 1];

            _poolId = poolId;
            _segmentArraySize = segmentArraySize;
            _buffers = new int[numberOfBuffers][];
            _levels = levels;
            _currentLevel = 0;

            if (preAllocate)
            {
                var stableLevel = levels[0];
                for (var i = 0; i < stableLevel; i++)
                {
                    _buffers[i] = new int[_segmentArraySize];
                }
            }

            _lock = new SpinLock(Debugger.IsAttached);
            _index = 0;
            _inUse = 0;

            _trimTimer = new Timer(static b => ((Bucket)b!).Trim(), this, trimInterval, trimInterval);
        }

        internal int InUse => _inUse;

        internal int[]? Rent()
        {
            Interlocked.Increment(ref _inUse);

            var buffers = _buffers;
            int[]? buffer = null;

            var lockTaken = false;
            var allocateBuffer = false;

            try
            {
                _lock.Enter(ref lockTaken);

                if (_index >= _levels[_currentLevel] && _currentLevel < _levels.Length - 1)
                {
                    _currentLevel++;
                }

                if (_index < _levels[_currentLevel])
                {
                    buffer = buffers[_index];
                    buffers[_index++] = null;
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

            if (allocateBuffer)
            {
                buffer = new int[_segmentArraySize];

                var log = Log;
                if (log.IsEnabled())
                {
                    log.SegmentAllocated(buffer.GetHashCode(), _segmentArraySize, _poolId);
                }
            }

            return buffer;
        }

        internal bool Return(int[] array)
        {
            Interlocked.Decrement(ref _inUse);

            if (array.Length != _segmentArraySize)
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
                    _buffers[--_index] = array;
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

        private void Trim()
        {
            var currentLevel = _currentLevel;

            if (currentLevel == 0)
            {
                return;
            }

            var previousLevel = currentLevel - 1;
            var previousLimit = _levels[previousLevel];

            if (_inUse > previousLimit)
            {
                return;
            }

            var trimmed = 0;
            var lockTaken = false;

            try
            {
                var currentLimit = _levels[currentLevel];

                _lock.Enter(ref lockTaken);

                for (var i = previousLimit; i < currentLimit; i++)
                {
                    if (_buffers[i] != null)
                    {
                        _buffers[i] = null;
                        trimmed++;
                    }
                }

                if (_index > previousLimit)
                {
                    _index = previousLimit;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }

            _currentLevel = previousLevel;

            var log = Log;
            if (log.IsEnabled())
            {
                log.PoolTrimmed(_poolId, trimmed, previousLimit, _inUse);
            }
        }

        public void Dispose() => _trimTimer.Dispose();
    }
}
