namespace HotChocolate.Buffers;

/// <summary>
/// Represents a contiguous region of memory that an <see cref="IMemoryArena"/> has handed out.
/// </summary>
public readonly struct MemorySegment
{
    /// <summary>
    /// Initializes a new instance of <see cref="MemorySegment"/>.
    /// </summary>
    /// <param name="buffer">The page that backs this segment.</param>
    /// <param name="offset">The offset at which the segment begins within <paramref name="buffer"/>.</param>
    /// <param name="length">The length of the segment in bytes.</param>
    public MemorySegment(byte[] buffer, int offset, int length)
    {
        Buffer = buffer;
        Offset = offset;
        Length = length;
    }

    /// <summary>
    /// Gets the page that backs this segment.
    /// </summary>
    public byte[] Buffer { get; }

    /// <summary>
    /// Gets the offset at which this segment begins within <see cref="Buffer"/>.
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// Gets the length of this segment in bytes.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Gets the segment as a span.
    /// </summary>
    public Span<byte> Span => Buffer.AsSpan(Offset, Length);

    /// <summary>
    /// Gets the segment as a memory.
    /// </summary>
    public Memory<byte> Memory => Buffer.AsMemory(Offset, Length);
}
