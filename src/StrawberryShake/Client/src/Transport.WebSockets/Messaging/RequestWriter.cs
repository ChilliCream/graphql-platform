using System.Buffers;

namespace StrawberryShake.Transport.WebSockets;

/// <inheritdoc />
public class RequestWriter
    : IRequestWriter
{
    private const int InitialBufferSize = 1024;
    private const int DefaultMemorySize = 256;
    private const int MinMemorySize = 1;
    private byte[] _buffer;
    private int _capacity;
    private int _start;
    private bool _disposed;

    protected RequestWriter()
    {
        _buffer = ArrayPool<byte>.Shared.Rent(InitialBufferSize);
        _capacity = _buffer.Length;
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> Body => _buffer.AsMemory().Slice(0, _start);

    /// <inheritdoc />
    public void Advance(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        _start += count;
        _capacity -= count;
    }

    /// <inheritdoc />
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        var size = sizeHint < MinMemorySize ? DefaultMemorySize : sizeHint;
        EnsureBufferCapacity(size);
        return _buffer.AsMemory().Slice(_start, size);
    }

    /// <inheritdoc />
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        var size = sizeHint < MinMemorySize ? DefaultMemorySize : sizeHint;
        EnsureBufferCapacity(size);
        return _buffer.AsSpan().Slice(_start, size);
    }

    private void EnsureBufferCapacity(int neededCapacity)
    {
        if (_capacity < neededCapacity)
        {
            var buffer = _buffer;

            var newSize = buffer.Length * 2;
            if (neededCapacity > buffer.Length)
            {
                newSize += neededCapacity;
            }

            _buffer = ArrayPool<byte>.Shared.Rent(newSize);
            _capacity += _buffer.Length - buffer.Length;

            buffer.AsSpan().CopyTo(_buffer);
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <inheritdoc />
    public virtual void Reset()
    {
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = ArrayPool<byte>.Shared.Rent(InitialBufferSize);
        _capacity = _buffer.Length;
        _start = 0;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting
    /// unmanaged resources
    /// </summary>
    /// <param name="disposing">True if it is disposing</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = [];
            _disposed = true;
        }
    }
}
