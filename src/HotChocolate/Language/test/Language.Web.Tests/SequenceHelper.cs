using System.Buffers;

namespace HotChocolate.Language;

internal static class SequenceHelper
{
    public static ReadOnlySequence<byte> CreateMultiSegment(byte[] data)
    {
        if (data.Length < 2)
        {
            return new ReadOnlySequence<byte>(data);
        }

        var mid = data.Length / 2;
        var first = new MemorySegment(data.AsMemory(0, mid));
        var last = first.Append(data.AsMemory(mid));

        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    private sealed class MemorySegment : ReadOnlySequenceSegment<byte>
    {
        public MemorySegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public MemorySegment Append(ReadOnlyMemory<byte> memory)
        {
            var segment = new MemorySegment(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };
            Next = segment;
            return segment;
        }
    }
}
