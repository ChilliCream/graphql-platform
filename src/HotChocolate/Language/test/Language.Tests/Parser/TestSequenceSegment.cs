using System.Buffers;

namespace HotChocolate.Language;

/// <summary>
/// A test helper that creates a multi-segment ReadOnlySequence from a byte array.
/// </summary>
internal sealed class TestSequenceSegment : ReadOnlySequenceSegment<byte>
{
    public TestSequenceSegment(ReadOnlyMemory<byte> memory, long runningIndex)
    {
        Memory = memory;
        RunningIndex = runningIndex;
    }

    public TestSequenceSegment Append(ReadOnlyMemory<byte> memory)
    {
        var next = new TestSequenceSegment(memory, RunningIndex + Memory.Length);
        Next = next;
        return next;
    }

    /// <summary>
    /// Creates a multi-segment ReadOnlySequence by splitting the input into chunks of the given size.
    /// </summary>
    public static ReadOnlySequence<byte> CreateMultiSegment(byte[] data, int chunkSize)
    {
        if (data.Length == 0)
        {
            throw new ArgumentException("Data cannot be empty.", nameof(data));
        }

        if (chunkSize <= 0)
        {
            throw new ArgumentException("Chunk size must be positive.", nameof(chunkSize));
        }

        if (chunkSize >= data.Length)
        {
            // Single segment
            return new ReadOnlySequence<byte>(data);
        }

        var first = new TestSequenceSegment(
            new ReadOnlyMemory<byte>(data, 0, Math.Min(chunkSize, data.Length)), 0);
        var current = first;
        var offset = chunkSize;

        while (offset < data.Length)
        {
            var length = Math.Min(chunkSize, data.Length - offset);
            current = current.Append(new ReadOnlyMemory<byte>(data, offset, length));
            offset += length;
        }

        return new ReadOnlySequence<byte>(first, 0, current, current.Memory.Length);
    }

    /// <summary>
    /// Creates a multi-segment ReadOnlySequence by splitting at the specified positions.
    /// </summary>
    public static ReadOnlySequence<byte> CreateMultiSegment(byte[] data, params int[] splitPositions)
    {
        if (data.Length == 0)
        {
            throw new ArgumentException("Data cannot be empty.", nameof(data));
        }

        if (splitPositions.Length == 0)
        {
            return new ReadOnlySequence<byte>(data);
        }

        var positions = splitPositions.OrderBy(p => p).Where(p => p > 0 && p < data.Length).Distinct().ToArray();
        if (positions.Length == 0)
        {
            return new ReadOnlySequence<byte>(data);
        }

        var first = new TestSequenceSegment(
            new ReadOnlyMemory<byte>(data, 0, positions[0]), 0);
        var current = first;

        for (var i = 0; i < positions.Length; i++)
        {
            var start = positions[i];
            var end = i + 1 < positions.Length ? positions[i + 1] : data.Length;
            current = current.Append(new ReadOnlyMemory<byte>(data, start, end - start));
        }

        return new ReadOnlySequence<byte>(first, 0, current, current.Memory.Length);
    }
}
