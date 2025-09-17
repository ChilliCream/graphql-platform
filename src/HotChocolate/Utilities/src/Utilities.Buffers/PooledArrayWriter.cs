using System.Buffers;
using static HotChocolate.Buffers.PooledArrayWriterEventSource;
#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
using static HotChocolate.Buffers.Properties.BuffersResources;

namespace HotChocolate.Buffers;

/// <summary>
/// A <see cref="IBufferWriter{T}"/> that writes to a rented buffer.
/// </summary>
public sealed class PooledArrayWriter : IWritableMemory
{
    private const int InitialBufferSize = 4096;
    private const int LargeAllocationThreshold = 1024 * 1024; // 1MB

    private byte[] _buffer;
    private int _capacity;
    private int _start;
    private int _resizeCount;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledArrayWriter"/> class.
    /// </summary>
    public PooledArrayWriter()
    {
        _buffer = BufferPools.Rent(InitialBufferSize);
        _capacity = _buffer.Length;
        _start = 0;
        _resizeCount = 0;

        Log.WriterCreated(InitialBufferSize, _capacity);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledArrayWriter"/> class.
    /// </summary>
    /// <param name="initialBufferSize">
    /// The initial buffer size.
    /// </param>
    public PooledArrayWriter(int initialBufferSize)
    {
        _buffer = BufferPools.Rent(initialBufferSize);
        _capacity = _buffer.Length;
        _start = 0;
        _resizeCount = 0;

        Log.WriterCreated(initialBufferSize, _capacity);

        if (initialBufferSize > LargeAllocationThreshold)
        {
            Log.LargeAllocation(initialBufferSize, LargeAllocationThreshold);
        }
    }

    /// <summary>
    /// Gets the number of bytes written to the buffer.
    /// </summary>
    public int Length => _start;

    /// <summary>
    /// Gets the current internal capacity of the internal buffer.
    /// </summary>
    public int Capacity => _buffer.Length;

    /// <summary>
    /// Gets the total number of buffer resizes that have occurred.
    /// </summary>
    public int ResizeCount => _resizeCount;

    /// <summary>
    /// Gets the underlying buffer.
    /// </summary>
    /// <returns>
    /// The underlying buffer.
    /// </returns>
    /// <remarks>
    /// Accessing the underlying buffer directly is not recommended.
    /// If possible use <see cref="WrittenMemory"/> or <see cref="WrittenSpan"/>.
    /// </remarks>
    internal byte[] GetInternalBuffer() => _buffer;

    /// <summary>
    /// Gets access to the full underlying buffer.
    /// </summary>
    public Memory<byte> Memory
        => _buffer;

    /// <summary>
    /// Gets the part of the buffer that has been written to.
    /// </summary>
    /// <returns>
    /// A <see cref="ReadOnlyMemory{T}"/> of the written portion of the buffer.
    /// </returns>
    public ReadOnlyMemory<byte> WrittenMemory
#if NETSTANDARD2_0
        => _buffer.AsMemory().Slice(0, _start);
#else
        => _buffer.AsMemory()[.._start];
#endif

    /// <summary>
    /// Gets the part of the buffer that has been written to.
    /// </summary>
    /// <returns>
    /// A <see cref="ReadOnlySpan{T}"/> of the written portion of the buffer.
    /// </returns>
    public ReadOnlySpan<byte> WrittenSpan
#if NETSTANDARD2_0
        => _buffer.AsSpan(0, _start);
#else
        => MemoryMarshal.CreateSpan(ref _buffer[0], _start);
#endif

    /// <summary>
    /// Gets the buffer as an <see cref="ArraySegment{T}"/>
    /// </summary>
    /// <returns>
    /// A <see cref="ArraySegment{T}"/> to reference to a certain part of the written memory.
    /// </returns>
    public ArraySegment<byte> WrittenArraySegment
        => new(_buffer, 0, _start);

    /// <summary>
    /// Gets a read-only memory segment to reference to a certain part of the written memory.
    /// </summary>
    /// <param name="start">
    /// The start index of the memory segment.
    /// </param>
    /// <param name="length">
    /// The length of the memory segment.
    /// </param>
    /// <returns>
    /// A <see cref="ReadOnlyMemorySegment"/> to reference to a certain part of the written memory.
    /// </returns>
    public ReadOnlyMemorySegment GetWrittenMemorySegment(int start, int length)
        => new(this, start, length);

    /// <summary>
    /// Advances the writer by the specified number of bytes.
    /// </summary>
    /// <param name="count">
    /// The number of bytes to advance the writer by.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="count"/> is negative or
    /// if <paramref name="count"/> is greater than the
    /// available capacity on the internal buffer.
    /// </exception>
    public void Advance(int count)
    {
#if NETSTANDARD2_0
        if (_disposed)
        {
            throw new ObjectDisposedException(typeof(PooledArrayWriter).FullName!);
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
#else
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
#endif

        if (count > _capacity)
        {
            Log.BufferOverflow(count, _capacity);
            throw new ArgumentOutOfRangeException(
                nameof(count),
                count,
                ArrayWriter_Advance_BufferOverflow);
        }

        _start += count;
        _capacity -= count;

        Log.WriterAdvanced(count, _start, _capacity);
    }

    /// <summary>
    /// Gets a <see cref="Memory{T}"/> to write to.
    /// </summary>
    /// <param name="sizeHint">
    /// The minimum size of the returned <see cref="Memory{T}"/>.
    /// </param>
    /// <returns>
    /// A <see cref="Memory{T}"/> to write to.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="sizeHint"/> is negative.
    /// </exception>
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
#if NETSTANDARD2_0
        if (_disposed)
        {
            throw new ObjectDisposedException(typeof(PooledArrayWriter).FullName!);
        }

        if (sizeHint < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeHint));
        }
#else
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);
#endif

        var size = sizeHint < 1 ? InitialBufferSize : sizeHint;
        var resizeRequired = _capacity < size;

        EnsureBufferCapacity(size);

        Log.MemoryRequested(sizeHint, size, resizeRequired);

        return _buffer.AsMemory().Slice(_start, size);
    }

    /// <summary>
    /// Gets a <see cref="Span{T}"/> to write to.
    /// </summary>
    /// <param name="sizeHint">
    /// The minimum size of the returned <see cref="Span{T}"/>.
    /// </param>
    /// <returns>
    /// A <see cref="Span{T}"/> to write to.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="sizeHint"/> is negative.
    /// </exception>
    public Span<byte> GetSpan(int sizeHint = 0)
    {
#if NETSTANDARD2_0
        if (_disposed)
        {
            throw new ObjectDisposedException(typeof(PooledArrayWriter).FullName!);
        }

        if (sizeHint < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeHint));
        }
#else
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);
#endif

        var size = sizeHint < 1 ? InitialBufferSize : sizeHint;
        var resizeRequired = _capacity < size;

        EnsureBufferCapacity(size);

        Log.MemoryRequested(sizeHint, size, resizeRequired);

#if NETSTANDARD2_0
        return _buffer.AsSpan(_start, size);
#else
        return MemoryMarshal.CreateSpan(ref _buffer[_start], size);
#endif
    }

    /// <summary>
    /// Ensures that the internal buffer has the necessary capacity.
    /// </summary>
    /// <param name="neededCapacity">
    /// The necessary capacity on the internal buffer.
    /// </param>
    public void EnsureBufferCapacity(int neededCapacity)
    {
        // check if we have enough capacity available on the buffer.
        if (_capacity < neededCapacity)
        {
            // if we need to expand the buffer, we first capture the original buffer.
            var buffer = _buffer;
            var oldSize = buffer.Length;

            // next we determine the new size of the buffer, we at least double the size to avoid
            // expanding the buffer too often.
            var newSize = buffer.Length * 2;

            // if that new buffer size is not enough to satisfy the necessary capacity,
            // we add the necessary capacity to the doubled buffer capacity.
            if (neededCapacity > newSize - _start)
            {
                newSize += neededCapacity;
            }

            // next we will rent a new array from the array pool that supports
            // the new capacity requirements.
            _buffer = BufferPools.Rent(newSize);
            var actualNewSize = _buffer.Length;

            // the rented array might have a larger size than the necessary capacity,
            // so we will take the buffer length and calculate from that the free capacity.
            _capacity += _buffer.Length - buffer.Length;

            // finally, we copy the data from the original buffer to the new buffer.
            buffer.AsSpan().CopyTo(_buffer);

            // last but not least, we return the original buffer to the array pool.
            BufferPools.Return(buffer);

            _resizeCount++;

            // Log the resize operation
            Log.BufferResize(oldSize, actualNewSize, _start, neededCapacity, _resizeCount);

            if (actualNewSize > LargeAllocationThreshold)
            {
                Log.LargeAllocation(actualNewSize, LargeAllocationThreshold);
            }
        }
    }

    public void Reset()
    {
        var previousLength = _start;
        _capacity = _buffer.Length;
        _start = 0;

        Log.WriterReset(previousLength, _capacity);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            Log.WriterDisposed(_start, _buffer.Length, _resizeCount);

            if (_start > 0)
            {
                _buffer.AsSpan(0, _start).Clear();
            }

            BufferPools.Return(_buffer);
            _buffer = [];
            _capacity = 0;
            _start = 0;
            _disposed = true;
        }
    }
}
