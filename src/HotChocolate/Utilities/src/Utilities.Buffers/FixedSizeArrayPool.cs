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

    public void Dispose() => _bucket.Dispose();

    private sealed class Bucket : IDisposable
    {
        private readonly int _poolId;
        private readonly int _bufferLength;
        private readonly byte[]?[] _buffers;
        private readonly int[] _levels;
        private readonly Timer _trimTimer;
        private int _currentLevel;
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
            var numberOfBuffers = levels[levels.Length - 1];

            _poolId = poolId;
            _bufferLength = bufferLength;
            _buffers = new byte[numberOfBuffers][];
            _levels = levels;
            _currentLevel = 0;

            if (preAllocate)
            {
                // only pre-allocate up to the stable level (first entry in _levels).
                var stableLevel = levels[0];
                for (var i = 0; i < stableLevel; i++)
                {
                    _buffers[i] = new byte[_bufferLength];
                }
            }

            _lock = new SpinLock(Debugger.IsAttached);
            _index = 0;
            _inUse = 0;

            _trimTimer = new Timer(static b => ((Bucket)b!).Trim(), this, trimInterval, trimInterval);
        }

        internal int InUse => _inUse;

        internal byte[]? Rent()
        {
            Interlocked.Increment(ref _inUse);

            var buffers = _buffers;
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

        // called from the TrimCallback timer once per minute.
        // if the pool is not under pressure we step the current level down one
        // notch and null out the buffer slots between the new and old level so
        // those byte[] arrays become eligible for collection.
        private void Trim()
        {
            var currentLevel = _currentLevel;

            // nothing to trim if we are already at the stable level.
            if (currentLevel == 0)
            {
                return;
            }

            var previousLevel = currentLevel - 1;
            var previousLimit = _levels[previousLevel];

            // if outstanding buffers exceed the target level the pool is still
            // under pressure — skip trimming.
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

                // null out slots between the new limit and the old limit.
                // only null slots that are beyond _index (i.e. not currently
                // rented out — those slots are already null).
                for (var i = previousLimit; i < currentLimit; i++)
                {
                    if (_buffers[i] != null)
                    {
                        _buffers[i] = null;
                        trimmed++;
                    }
                }

                // if _index is beyond the new limit we need to pull it back
                // and release those buffers too.
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
