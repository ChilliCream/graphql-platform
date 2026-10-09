using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotChocolate.Buffers;

/// <summary>
/// A segment of memory that is either backed by an <see cref="IMemoryOwner{T}"/> or refers to
/// memory whose lifetime the caller manages.
/// </summary>
public readonly struct ReadOnlyMemorySegment
{
    // either null, a byte[] or an IMemoryOwner<byte>.
    private readonly object? _source;
    private readonly int _start;
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
        _source = owner;
        _start = start;
        _length = length;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ReadOnlyMemorySegment"/> that refers to
    /// the whole <paramref name="array"/>.
    /// </summary>
    /// <param name="array">
    /// The array the segment refers to.
    /// </param>
    public ReadOnlyMemorySegment(byte[] array)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(array);
#else
        if (array is null)
        {
            throw new ArgumentNullException(nameof(array));
        }
#endif

        _source = array;
        _length = array.Length;
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
        if (memory.Equals(default))
        {
            return;
        }

        if (MemoryMarshal.TryGetArray(memory, out var segment))
        {
            _source = segment.Array;
            _start = segment.Offset;
        }
        else if (MemoryMarshal.TryGetMemoryManager(memory, out MemoryManager<byte>? manager, out var start, out _))
        {
            _source = manager;
            _start = start;
        }

        _length = memory.Length;
    }

    /// <summary>
    /// Gets a value indicating whether the segment has no backing memory.
    /// </summary>
    public bool IsEmpty => _source is null;

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
            var source = _source;

            if (source is byte[] array)
            {
                return new ReadOnlyMemory<byte>(array, _start, _length);
            }

            if (source is null)
            {
                return default;
            }

            return Unsafe.As<IMemoryOwner<byte>>(source).Memory.Slice(_start, _length);
        }
    }

    /// <summary>
    /// Gets the memory segment as a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public ReadOnlySpan<byte> Span
    {
        get
        {
            var source = _source;

            if (source is byte[] array)
            {
                return new ReadOnlySpan<byte>(array, _start, _length);
            }

            if (source is null)
            {
                return default;
            }

            return Unsafe.As<IMemoryOwner<byte>>(source).Memory.Span.Slice(_start, _length);
        }
    }
}
