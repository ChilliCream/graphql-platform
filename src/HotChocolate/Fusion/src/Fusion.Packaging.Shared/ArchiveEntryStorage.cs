using System.Buffers;

namespace HotChocolate.Fusion.Packaging.Storage;

internal enum ArchiveEntryStorageKind
{
    Memory,
    TempFile
}

internal abstract class ArchiveEntryStorage : IDisposable
{
    public abstract long Length { get; }

    public abstract Stream OpenRead();

    public abstract Stream OpenWrite(int maximumLength, Action<long>? completed = null);

    public abstract Task CopyToAsync(Stream destination, CancellationToken cancellationToken);

    public abstract void Dispose();

    public static ArchiveEntryStorage Create(ArchiveEntryStorageKind kind)
        => kind switch
        {
            ArchiveEntryStorageKind.Memory => new PooledMemoryArchiveEntryStorage(),
            ArchiveEntryStorageKind.TempFile => new TempFileArchiveEntryStorage(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
}

internal sealed class TempFileArchiveEntryStorage : ArchiveEntryStorage
{
    private readonly string _path = Path.Combine(
        Path.GetTempPath(),
        $"hotchocolate-fusion-{Environment.ProcessId}-{Guid.NewGuid():N}.tmp");

    public override long Length
        => File.Exists(_path) ? new FileInfo(_path).Length : 0;

    public override Stream OpenRead()
        => File.OpenRead(_path);

    public override Stream OpenWrite(int maximumLength, Action<long>? completed = null)
        => File.Open(_path, FileMode.Create, FileAccess.Write);

    public override async Task CopyToAsync(
        Stream destination,
        CancellationToken cancellationToken)
    {
        await using var source = OpenRead();
        await source.CopyToAsync(destination, cancellationToken);
    }

    public override void Dispose()
    {
        if (!File.Exists(_path))
        {
            return;
        }

        try
        {
            File.Delete(_path);
        }
        catch
        {
            // Temp-file cleanup is best effort.
        }
    }
}

internal sealed class PooledMemoryArchiveEntryStorage : ArchiveEntryStorage
{
    private readonly PooledBuffer _buffer;

    public PooledMemoryArchiveEntryStorage()
        : this(ArrayPool<byte>.Shared)
    {
    }

    internal PooledMemoryArchiveEntryStorage(ArrayPool<byte> pool)
    {
        _buffer = new PooledBuffer(pool);
    }

    public override long Length => _buffer.Length;

    public override Stream OpenRead()
        => new PooledBufferReadStream(_buffer.WrittenMemory);

    public override Stream OpenWrite(int maximumLength, Action<long>? completed = null)
    {
        _buffer.Reset(maximumLength);
        return new PooledBufferWriteStream(_buffer, completed);
    }

    public override Task CopyToAsync(
        Stream destination,
        CancellationToken cancellationToken)
        => destination.WriteAsync(_buffer.WrittenMemory, cancellationToken).AsTask();

    public override void Dispose()
        => _buffer.Dispose();
}

internal sealed class PooledBuffer : IDisposable
{
    private readonly ArrayPool<byte> _pool;
    private byte[]? _buffer;
    private int _length;
    private int _maximumLength;
    private bool _disposed;

    public PooledBuffer(ArrayPool<byte> pool)
    {
        ArgumentNullException.ThrowIfNull(pool);
        _pool = pool;
    }

    public int Length => _length;

    public ReadOnlyMemory<byte> WrittenMemory
        => _buffer is null ? ReadOnlyMemory<byte>.Empty : _buffer.AsMemory(0, _length);

    public void Reset(int maximumLength)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(maximumLength);

        if (_buffer is not null && _length > 0)
        {
            _buffer.AsSpan(0, _length).Clear();
        }

        _length = 0;
        _maximumLength = maximumLength;
    }

    public void Write(ReadOnlySpan<byte> source)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var requiredLength = checked(_length + source.Length);

        if (requiredLength > _maximumLength)
        {
            throw new InvalidOperationException(
                $"File is too large and exceeds the allowed size of {_maximumLength}.");
        }

        EnsureCapacity(requiredLength);
        source.CopyTo(_buffer.AsSpan(_length));
        _length = requiredLength;
    }

    private void EnsureCapacity(int requiredLength)
    {
        if (requiredLength is 0 || (_buffer?.Length ?? 0) >= requiredLength)
        {
            return;
        }

        var currentLength = _buffer?.Length ?? 0;
        var newLength = Math.Min(
            _maximumLength,
            Math.Max(requiredLength, currentLength is 0 ? 4096 : checked(currentLength * 2)));
        var newBuffer = _pool.Rent(newLength);

        if (_buffer is not null)
        {
            _buffer.AsSpan(0, _length).CopyTo(newBuffer);
            _pool.Return(_buffer, clearArray: true);
        }

        _buffer = newBuffer;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_buffer is not null)
        {
            _pool.Return(_buffer, clearArray: true);
            _buffer = null;
        }

        _length = 0;
        _disposed = true;
    }
}

internal sealed class PooledBufferWriteStream(
    PooledBuffer buffer,
    Action<long>? completed) : Stream
{
    private bool _disposed;

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => !_disposed;

    public override long Length => buffer.Length;

    public override long Position
    {
        get => buffer.Length;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException();

    public override void SetLength(long value)
        => throw new NotSupportedException();

    public override void Write(byte[] source, int offset, int count)
        => Write(source.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> source)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        buffer.Write(source);
    }

    public override Task WriteAsync(
        byte[] source,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Write(source.AsSpan(offset, count));
        return Task.CompletedTask;
    }

    public override ValueTask WriteAsync(
        ReadOnlyMemory<byte> source,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Write(source.Span);
        return ValueTask.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            completed?.Invoke(buffer.Length);
        }

        base.Dispose(disposing);
    }
}

internal sealed class PooledBufferReadStream(ReadOnlyMemory<byte> memory) : Stream
{
    private int _position;

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => memory.Length;

    public override long Position
    {
        get => _position;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, memory.Length);

            _position = checked((int)value);
        }
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] destination, int offset, int count)
        => Read(destination.AsSpan(offset, count));

    public override int Read(Span<byte> destination)
    {
        var bytesToCopy = Math.Min(destination.Length, memory.Length - _position);
        memory.Span.Slice(_position, bytesToCopy).CopyTo(destination);
        _position += bytesToCopy;
        return bytesToCopy;
    }

    public override Task<int> ReadAsync(
        byte[] destination,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Read(destination.AsSpan(offset, count)));
    }

    public override ValueTask<int> ReadAsync(
        Memory<byte> destination,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Read(destination.Span));
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => checked(_position + offset),
            SeekOrigin.End => checked(memory.Length + offset),
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
        };

        Position = position;
        return position;
    }

    public override void SetLength(long value)
        => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();
}
