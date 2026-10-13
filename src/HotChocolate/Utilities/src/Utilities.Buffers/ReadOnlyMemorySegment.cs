using System.Buffers;
using System.Runtime.CompilerServices;

namespace HotChocolate.Buffers;

/// <summary>
/// A segment of memory that is either backed by an <see cref="IMemoryOwner{T}"/> or refers to
/// memory whose lifetime the caller manages.
/// </summary>
public readonly struct ReadOnlyMemorySegment
{
    // Contains either a memory owner or a complete byte array.
    private readonly object? _ownerOrArray;
    private readonly ReadOnlyMemory<byte> _memory;
    private readonly int _start;
    // Owner lengths are complemented; whole-array segments leave all other fields at default.
    private readonly int _length;

    /// <summary>
    /// Initializes a new instance of <see cref="ReadOnlyMemorySegment"/> that resolves against the
    /// current memory of <paramref name="owner"/>.
    /// </summary>
    /// <param name="owner">
    /// The owner whose memory backs the segment.
    /// </param>
    /// <param name="start">
    /// The start index of the segment within the memory of <paramref name="owner"/>.
    /// </param>
    /// <param name="length">
    /// The length of the segment.
    /// </param>
    public ReadOnlyMemorySegment(IMemoryOwner<byte> owner, int start, int length)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentOutOfRangeException.ThrowIfLessThan(start, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 0);
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
        // we have start and length here so that we can lazily slice the memory.
        // this allows us in combination with the Utf8MemoryBuilder to create
        // a memory segment before the memory is actually written to.
        _ownerOrArray = owner;
        _start = start;
        _length = ~length;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ReadOnlyMemorySegment"/> that refers to memory
    /// whose lifetime the caller manages.
    /// </summary>
    /// <param name="memory">
    /// The memory the segment refers to, which must stay valid for as long as the segment is used.
    /// </param>
    public ReadOnlyMemorySegment(ReadOnlyMemory<byte> memory)
    {
        _memory = memory;
        _length = memory.Length;
    }

    /// <summary>
    /// Initializes a segment over the entire array. A null array has no backing memory.
    /// </summary>
    public ReadOnlyMemorySegment(byte[]? memory)
    {
        _ownerOrArray = memory;
        _memory = new ReadOnlyMemory<byte>(null);
        _start = 0;
        _length = 0;
    }

    /// <summary>
    /// Gets a value indicating whether the segment has no backing memory.
    /// </summary>
    public bool IsEmpty => _ownerOrArray is null && _memory.Equals(default);

    /// <summary>
    /// Gets the length of the memory segment.
    /// </summary>
    public int Length => _length < 0
        ? ~_length
        : _ownerOrArray is { } array
            ? Unsafe.As<byte[]>(array).Length
            : _length;

    /// <summary>
    /// Gets the memory segment as a <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    public ReadOnlyMemory<byte> Memory
    {
        get
        {
            var ownerOrArray = _ownerOrArray;
            if (ownerOrArray is null)
            {
                return _memory;
            }

            return _length < 0
                ? Unsafe.As<IMemoryOwner<byte>>(ownerOrArray).Memory.Slice(_start, ~_length)
                : Unsafe.As<byte[]>(ownerOrArray);
        }
    }

    /// <summary>
    /// Gets the memory segment as a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public ReadOnlySpan<byte> Span
    {
        get
        {
            var ownerOrArray = _ownerOrArray;
            if (ownerOrArray is null)
            {
                return _memory.Span;
            }

            return _length < 0
                ? Unsafe.As<IMemoryOwner<byte>>(ownerOrArray).Memory.Span.Slice(_start, ~_length)
                : Unsafe.As<byte[]>(ownerOrArray);
        }
    }
}
