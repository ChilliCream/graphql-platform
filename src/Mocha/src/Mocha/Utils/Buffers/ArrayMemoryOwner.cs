using System.Buffers;

namespace Mocha.Utils;

/// <summary>
/// A memory owner for a byte array.
/// </summary>
public sealed class ArrayMemoryOwner : IMemoryOwner<byte>
{
    private readonly byte[] _buffer;
    private readonly int _start;
    private readonly int _length;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayMemoryOwner"/> class that wraps the entire buffer.
    /// </summary>
    /// <param name="buffer">The byte array to wrap.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="buffer"/> is <c>null</c>.</exception>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayMemoryOwner"/> class that wraps a segment of the buffer.
    /// </summary>
    /// <param name="buffer">The byte array to wrap.</param>
    /// <param name="start">The start index within the buffer.</param>
    /// <param name="length">The length of the segment.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="buffer"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="start"/> or <paramref name="length"/> is negative.</exception>
    public ArrayMemoryOwner(byte[] buffer, int start, int length)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfLessThan(start, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 0);
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

    /// <inheritdoc />
    public Memory<byte> Memory => _buffer.AsMemory().Slice(_start, _length);

    /// <inheritdoc />
    public void Dispose()
    {
        // do nothing
    }
}
