using System.Diagnostics;
using static HotChocolate.Buffers.FixedSizeArrayPoolEventSource;
using static HotChocolate.Buffers.Properties.BuffersResources;

namespace HotChocolate.Buffers;

internal sealed class FixedSizeArrayPool
{
    private readonly int _poolId;
    private readonly int _arraySize;
    private readonly int _numberOfArrays;
    private readonly Bucket _bucket;

    public FixedSizeArrayPool(int poolId, int arraySize, int numberOfArrays, bool preAllocate = false)
    {
        _poolId = poolId;
        _arraySize = arraySize;
        _numberOfArrays = numberOfArrays;
        _bucket = new Bucket(poolId, arraySize, numberOfArrays, preAllocate);
        Log.PoolCreated(poolId, numberOfArrays, (long)numberOfArrays * arraySize);
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

    private sealed class Bucket
    {
        private readonly int _poolId;
        private readonly int _bufferLength;
        private readonly byte[]?[] _buffers;
        private SpinLock _lock;
        private int _index;

        internal Bucket(int poolId, int bufferLength, int numberOfBuffers, bool preAllocate)
        {
            _poolId = poolId;
            _bufferLength = bufferLength;
            _buffers = new byte[numberOfBuffers][];

            if (preAllocate)
            {
                for (var i = 0; i < _buffers.Length; i++)
                {
                    _buffers[i] = new byte[_bufferLength];
                }
            }

            _lock = new SpinLock(Debugger.IsAttached);
            _index = 0;
        }

        internal int InUse => _index;

        internal byte[]? Rent()
        {
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
    }
}
