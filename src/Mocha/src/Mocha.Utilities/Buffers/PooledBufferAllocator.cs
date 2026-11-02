using System.Buffers;

namespace Mocha.Utils;

/// <summary>
/// A bump allocator that carves <see cref="Memory{T}"/> slices from pooled byte arrays.
/// Multiple allocations share the same underlying buffer until it is full, at which point
/// a new buffer is rented. All rented buffers are returned to the pool on <see cref="Dispose"/>.
/// </summary>
/// <remarks>
/// Unlike <see cref="PooledArrayWriter"/>, which maintains a single contiguous buffer that
/// is resized (copied) on growth, this allocator never copies data. Each allocation returns
/// a slice of the current buffer, and when the current buffer is exhausted a fresh one is
/// rented. This makes it ideal for packing many independent data items (e.g. message bodies
/// and headers) into a small number of pooled buffers.
/// </remarks>
internal sealed class PooledBufferAllocator : IDisposable
{
    private const int DefaultMinBufferSize = 4 * 1024; // 4KB

    private readonly ArrayPool<byte> _pool;
    private readonly int _minBufferSize;
    private readonly List<byte[]> _rentedBuffers = [];

    private byte[]? _currentBuffer;
    private int _currentOffset;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledBufferAllocator"/> class
    /// using <see cref="ArrayPool{T}.Shared"/> and the default minimum buffer size (4KB).
    /// </summary>
    public PooledBufferAllocator() : this(ArrayPool<byte>.Shared, DefaultMinBufferSize) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledBufferAllocator"/> class
    /// using <see cref="ArrayPool{T}.Shared"/> and a custom minimum buffer size.
    /// </summary>
    /// <param name="minBufferSize">
    /// The minimum size of each rented buffer. Actual buffers may be larger
    /// depending on pool bucket sizes.
    /// </param>
    public PooledBufferAllocator(int minBufferSize) : this(ArrayPool<byte>.Shared, minBufferSize) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledBufferAllocator"/> class
    /// with a custom <see cref="ArrayPool{T}"/> and minimum buffer size.
    /// </summary>
    /// <param name="pool">The array pool to rent buffers from.</param>
    /// <param name="minBufferSize">
    /// The minimum size of each rented buffer. Actual buffers may be larger
    /// depending on pool bucket sizes.
    /// </param>
    public PooledBufferAllocator(ArrayPool<byte> pool, int minBufferSize = DefaultMinBufferSize)
    {
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minBufferSize);

        _pool = pool;
        _minBufferSize = minBufferSize;
    }

    /// <summary>
    /// Gets the total number of buffers currently rented from the pool.
    /// </summary>
    public int BufferCount => _rentedBuffers.Count;

    /// <summary>
    /// Returns a writable <see cref="Memory{T}"/> slice of exactly <paramref name="size"/>
    /// bytes. If the current buffer has enough remaining capacity the slice is carved from it;
    /// otherwise a new buffer is rented from the pool.
    /// </summary>
    /// <param name="size">The number of bytes to allocate.</param>
    /// <returns>A writable <see cref="Memory{T}"/> of exactly <paramref name="size"/> bytes.</returns>
    public Memory<byte> GetMemory(int size)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        if (_currentBuffer is not null && _currentOffset + size <= _currentBuffer.Length)
        {
            var memory = _currentBuffer.AsMemory(_currentOffset, size);
            _currentOffset += size;
            return memory;
        }

        var bufferSize = Math.Max(_minBufferSize, size);
        _currentBuffer = _pool.Rent(bufferSize);
        _currentOffset = size;
        _rentedBuffers.Add(_currentBuffer);

        return _currentBuffer.AsMemory(0, size);
    }

    /// <summary>
    /// Returns all rented buffers back to the pool.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var buffer in _rentedBuffers)
        {
            _pool.Return(buffer, clearArray: false);
        }

        _rentedBuffers.Clear();
        _currentBuffer = null;
        _currentOffset = 0;
        _disposed = true;
    }
}
