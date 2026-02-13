using System.Buffers;
using HotChocolate.Buffers;

namespace HotChocolate.Language;

internal sealed class Utf8MemoryBuilder : IWritableMemory
{
    private byte[] _buffer = [];
    private int _written;

    public int NextIndex
    {
        get
        {
            if (_written == -1)
            {
                throw new InvalidOperationException("Memory is sealed.");
            }

            return _written;
        }
    }

    public Memory<byte> Memory => _buffer.AsMemory();

    public ReadOnlySpan<byte> WrittenSpan
        => _written == -1 ? _buffer : _buffer.AsSpan(0, _written);

    public ReadOnlyMemory<byte> WrittenMemory
        => _written == -1 ? _buffer : _buffer.AsMemory(0, _written);

    public ReadOnlyMemorySegment Write(ReadOnlySpan<byte> value)
    {
        if (_written == -1)
        {
            throw new InvalidOperationException("Memory is sealed.");
        }

        EnsureCapacity(value.Length);
        var start = _written;
        var destination = _buffer.AsSpan().Slice(start);
        _written += value.Length;
        value.CopyTo(destination);
        return new ReadOnlyMemorySegment(this, start, value.Length);
    }

    public ReadOnlyMemorySegment GetMemorySegment(int start, int length)
        => new(this, start, length);

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (_written == -1)
        {
            throw new InvalidOperationException("Memory is sealed.");
        }

        if (sizeHint == 0)
        {
            sizeHint = 128;
        }

        EnsureCapacity(sizeHint);
        return _buffer.AsMemory().Slice(_written, sizeHint);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (_written == -1)
        {
            throw new InvalidOperationException("Memory is sealed.");
        }

        if (sizeHint == 0)
        {
            sizeHint = 128;
        }

        EnsureCapacity(sizeHint);
        return _buffer.AsSpan().Slice(_written, sizeHint);
    }

    public void Advance(int count)
    {
        if (_written == -1)
        {
            throw new InvalidOperationException("Memory is sealed.");
        }

        _written += count;
    }

    public void Seal()
    {
        if (_written == -1)
        {
            throw new InvalidOperationException("Memory is sealed.");
        }

        var finalArray = _written > 0
            ? _buffer.AsSpan().Slice(0, _written).ToArray()
            : [];
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = finalArray;
    }

    public void Abandon()
    {
        if (_written == -1)
        {
            throw new InvalidOperationException("Memory is sealed.");
        }

        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = [];
    }

    private void EnsureCapacity(int length)
    {
        var requiredCapacity = _written + length;

        if (_buffer.Length >= requiredCapacity)
        {
            return;
        }

        var newCapacity = _buffer.Length == 0 ? 1024 : _buffer.Length * 2;
        if (newCapacity < requiredCapacity)
        {
            newCapacity = requiredCapacity;
        }

        var newBuffer = ArrayPool<byte>.Shared.Rent(newCapacity);
        _buffer.AsSpan(0, _written).CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }

    public void Dispose()
    {
    }
}
