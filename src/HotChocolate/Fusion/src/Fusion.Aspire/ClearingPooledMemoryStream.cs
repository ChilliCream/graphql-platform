using System.Buffers;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// A seekable in-memory stream that clears every rented buffer before returning it.
/// </summary>
internal sealed class ClearingPooledMemoryStream : Stream
{
    private const int DefaultCapacity = 4096;
    private readonly ArrayPool<byte> _pool;
    private readonly int _maximumLength;
    private readonly string _maximumLengthErrorMessage;
    private byte[] _buffer;
    private int _length;
    private int _position;
    private bool _disposed;

    public ClearingPooledMemoryStream()
        : this(
            ArrayPool<byte>.Shared,
            DefaultCapacity,
            int.MaxValue,
            "The in-memory stream exceeds its maximum length.")
    {
    }

    internal ClearingPooledMemoryStream(ArrayPool<byte> pool, int initialCapacity)
        : this(
            pool,
            initialCapacity,
            int.MaxValue,
            "The in-memory stream exceeds its maximum length.")
    {
    }

    internal ClearingPooledMemoryStream(
        int maximumLength,
        string maximumLengthErrorMessage)
        : this(
            ArrayPool<byte>.Shared,
            Math.Min(DefaultCapacity, maximumLength),
            maximumLength,
            maximumLengthErrorMessage)
    {
    }

    private ClearingPooledMemoryStream(
        ArrayPool<byte> pool,
        int initialCapacity,
        int maximumLength,
        string maximumLengthErrorMessage)
    {
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialCapacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumLength);
        ArgumentException.ThrowIfNullOrWhiteSpace(maximumLengthErrorMessage);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCapacity, maximumLength);

        _pool = pool;
        _maximumLength = maximumLength;
        _maximumLengthErrorMessage = maximumLengthErrorMessage;
        _buffer = pool.Rent(initialCapacity);
    }

    public override bool CanRead => !_disposed;

    public override bool CanSeek => !_disposed;

    public override bool CanWrite => !_disposed;

    public override long Length
    {
        get
        {
            ThrowIfDisposed();
            return _length;
        }
    }

    public override long Position
    {
        get
        {
            ThrowIfDisposed();
            return _position;
        }
        set
        {
            ThrowIfDisposed();

            if (value is < 0 or > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _position = (int)value;
        }
    }

    public byte[] ToArray()
    {
        ThrowIfDisposed();
        return _buffer.AsSpan(0, _length).ToArray();
    }

    public override void Flush()
        => ThrowIfDisposed();

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        return Task.CompletedTask;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (buffer.Length - offset < count)
        {
            throw new ArgumentException("The offset and count exceed the buffer length.");
        }

        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();
        var count = Math.Min(buffer.Length, _length - _position);

        if (count <= 0)
        {
            return 0;
        }

        _buffer.AsSpan(_position, count).CopyTo(buffer);
        _position += count;
        return count;
    }

    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Read(buffer.Span));
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();
        var reference = origin switch
        {
            SeekOrigin.Begin => 0L,
            SeekOrigin.Current => _position,
            SeekOrigin.End => _length,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };
        var position = reference + offset;

        if (position is < 0 or > int.MaxValue)
        {
            throw new IOException("The stream position is outside the supported range.");
        }

        _position = (int)position;
        return _position;
    }

    public override void SetLength(long value)
    {
        ThrowIfDisposed();

        if (value is < 0 or > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        var length = (int)value;
        EnsureCapacity(length);

        if (length > _length)
        {
            _buffer.AsSpan(_length, length - _length).Clear();
        }
        else if (length < _length)
        {
            _buffer.AsSpan(length, _length - length).Clear();
        }

        _length = length;
        _position = Math.Min(_position, length);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (buffer.Length - offset < count)
        {
            throw new ArgumentException("The offset and count exceed the buffer length.");
        }

        Write(buffer.AsSpan(offset, count));
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();
        var end = checked(_position + buffer.Length);
        EnsureCapacity(end);

        if (_position > _length)
        {
            _buffer.AsSpan(_length, _position - _length).Clear();
        }

        buffer.CopyTo(_buffer.AsSpan(_position));
        _position = end;
        _length = Math.Max(_length, end);
    }

    public override ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Write(buffer.Span);
        return ValueTask.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _pool.Return(_buffer, clearArray: true);
            _buffer = [];
            _length = 0;
            _position = 0;
            _disposed = true;
        }

        base.Dispose(disposing);
    }

    private void EnsureCapacity(int capacity)
    {
        if (capacity > _maximumLength)
        {
            throw new InvalidDataException(_maximumLengthErrorMessage);
        }

        if (capacity <= _buffer.Length)
        {
            return;
        }

        var doubledCapacity = _buffer.Length > _maximumLength / 2
            ? _maximumLength
            : _buffer.Length * 2;
        var newCapacity = Math.Min(
            _maximumLength,
            Math.Max(capacity, doubledCapacity));
        var replacement = _pool.Rent(newCapacity);
        _buffer.AsSpan(0, _length).CopyTo(replacement);
        _pool.Return(_buffer, clearArray: true);
        _buffer = replacement;
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, this);
}
