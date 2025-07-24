using System.Buffers;
#if NET8_0_OR_GREATER
#endif

namespace HotChocolate.Buffers;

/// <summary>
/// A segment of memory that is owned by a <see cref="IMemoryOwner{T}"/>.
/// </summary>
public readonly struct ReadOnlyMemorySegment
{
    private readonly IMemoryOwner<byte> _owner;
    private readonly int _start;
    private readonly int _length;

    /// <summary>
    /// Initializes a new instance of <see cref="ReadOnlyMemorySegment"/>.
    /// </summary>
    /// <param name="owner">
    /// The owner of the memory segment.
    /// </param>
    /// <param name="start">
    /// The start index of the memory segment.
    /// </param>
    /// <param name="length">
    /// The length of the memory segment.
    /// </param>
    public ReadOnlyMemorySegment(IMemoryOwner<byte> owner, int start, int length)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentOutOfRangeException.ThrowIfLessThan(start, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(length, 0);
#else
        if (owner is null)
        {
            throw new ArgumentNullException(nameof(owner));
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

        _owner = owner;
        _start = start;
        _length = length;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ReadOnlyMemorySegment"/>.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to create the memory segment from.
    /// </param>
    public ReadOnlyMemorySegment(byte[] buffer)
        : this(buffer, 0, buffer.Length)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ReadOnlyMemorySegment"/>.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to create the memory segment from.
    /// </param>
    /// <param name="start">
    /// The start index of the memory segment.
    /// </param>
    /// <param name="length">
    /// The length of the memory segment.
    /// </param>
    public ReadOnlyMemorySegment(byte[] buffer, int start, int length)
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
        _owner = new ArrayMemoryOwner(buffer, start, length);
        _start = start;
        _length = length;
    }

    /// <summary>
    /// Gets a value indicating whether the memory segment is empty.
    /// </summary>
    public bool IsEmpty => _owner is null;

    /// <summary>
    /// Gets the length of the memory segment.
    /// </summary>
    public int Length => _length;

    /// <summary>
    /// Gets the memory segment as a <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    public ReadOnlyMemory<byte> Memory
    {
        get
        {
            return _owner is not null
                ? _owner.Memory.Slice(_start, _length)
                : Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Gets the memory segment as a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public ReadOnlySpan<byte> Span
    {
        get
        {
            return _owner is not null
                ? _owner.Memory.Span.Slice(_start, _length)
                : [];
        }
    }
}
