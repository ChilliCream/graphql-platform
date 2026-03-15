using System.Diagnostics;

namespace HotChocolate.Fusion.Text.Json;

internal sealed class PathSegmentPool : IDisposable
{
    internal readonly int _segmentArraySize;
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
        _bucket = new Bucket(segmentArraySize, levels, trimInterval, preAllocate);
    }

    public int[] Rent()
    {
        return _bucket.Rent() ?? new int[_segmentArraySize];
    }

    public void Return(int[] array)
    {
        if (array.Length != _segmentArraySize)
        {
            return;
        }

        _bucket.Return(array);
    }

    public void Dispose() => _bucket.Dispose();

    private sealed class Bucket : IDisposable
    {
        private readonly int _segmentArraySize;
        private readonly int[]?[] _buffers;
        private readonly int[] _levels;
        private readonly Timer _trimTimer;
        private int _currentLevel;
        private int _inUse;
        private SpinLock _lock;
        private int _index;

        internal Bucket(
            int segmentArraySize,
            int[] levels,
            TimeSpan trimInterval,
            bool preAllocate)
        {
            var numberOfBuffers = levels[levels.Length - 1];

            _segmentArraySize = segmentArraySize;
            _buffers = new int[numberOfBuffers][];
            _levels = levels;
            _currentLevel = _levels.Length - 1;

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

                if (_index < buffers.Length)
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
            }

            return buffer;
        }

        internal void Return(int[] array)
        {
            Interlocked.Decrement(ref _inUse);

            if (array.Length != _segmentArraySize)
            {
                return;
            }

            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);

                if (_index > 0)
                {
                    _buffers[--_index] = array;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }
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
        }

        public void Dispose() => _trimTimer.Dispose();
    }
}
