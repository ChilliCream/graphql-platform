using System.Buffers;

namespace HotChocolate.Fusion.Text.Json;

internal class SequenceSegment : ReadOnlySequenceSegment<byte>
{
    public SequenceSegment(byte[] memory, int length)
    {
        Memory = memory.AsMemory(0, length);
    }

    public SequenceSegment SetNext(SequenceSegment next)
    {
        Next = next;
        next.RunningIndex = RunningIndex + Memory.Length;
        return next;
    }
}
