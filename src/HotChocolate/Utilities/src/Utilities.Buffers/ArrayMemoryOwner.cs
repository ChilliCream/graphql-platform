using System.Buffers;

namespace HotChocolate.Buffers;

/// <summary>
/// A memory owner for a byte array.
/// </summary>
public sealed class ArrayMemoryOwner : IMemoryOwner<byte>
{
    private readonly byte[] _buffer;
    private readonly int _start;
    private readonly int _length;

    public ArrayMemoryOwner(byte[] buffer)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(buffer);
#else
        if (buffer is null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }
#endif

        _buffer = buffer;
        _start = 0;
        _length = buffer.Length;
    }

    public ArrayMemoryOwner(byte[] buffer, int start, int length)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfLessThan(start, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(length, 0);
#else
        if (buffer is null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }
#endif

        _buffer = buffer;
        _start = start;
        _length = length;
    }

    public Memory<byte> Memory => _buffer.AsMemory().Slice(_start, _length);

    public void Dispose()
    {
        // do nothing
    }
}
