using System.Buffers;

namespace HotChocolate.Buffers;

/// <summary>
/// A piece of memory in a <see cref="ReadOnlySequence{T}"/> that spans several pieces.
/// </summary>
internal sealed class MemorySequenceSegment : ReadOnlySequenceSegment<byte>
{
    public MemorySequenceSegment(ReadOnlyMemory<byte> memory)
    {
        Memory = memory;
    }

    /// <summary>
    /// Links <paramref name="memory"/> after this piece and returns the segment that holds it.
    /// </summary>
    public MemorySequenceSegment Append(ReadOnlyMemory<byte> memory)
    {
        var next = new MemorySequenceSegment(memory)
        {
            RunningIndex = RunningIndex + Memory.Length
        };
        Next = next;
        return next;
    }
}
