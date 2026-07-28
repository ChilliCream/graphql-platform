using System.Buffers;
#if NET8_0_OR_GREATER
#endif

namespace HotChocolate.Buffers;

/// <summary>
/// A segment of memory that is either owned by a <see cref="IMemoryOwner{T}"/>
/// or refers to memory whose lifetime is managed elsewhere.
/// </summary>
public readonly struct ReadOnlyMemorySegment
{
    private readonly IMemoryOwner<byte>? _owner;
    private readonly ReadOnlyMemory<byte> _memory;
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
        _owner = owner;
        _start = start;
        _length = length;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ReadOnlyMemorySegment"/>.
    /// </summary>
    /// <param name="memory">
    /// The memory the segment refers to.
    /// </param>
    public ReadOnlyMemorySegment(ReadOnlyMemory<byte> memory)
    {
        _memory = memory;
        _length = memory.Length;
    }

    /// <summary>
    /// Gets a value indicating whether the segment has no backing memory.
    /// </summary>
    public bool IsEmpty => _owner is null && _memory.Equals(default);

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
                : _memory;
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
                : _memory.Span;
        }
    }
}
