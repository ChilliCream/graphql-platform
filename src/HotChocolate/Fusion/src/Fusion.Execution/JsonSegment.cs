using System.Buffers;
using HotChocolate.Buffers;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion;

public readonly struct JsonSegment : IEquatable<JsonSegment>
{
    private readonly ReadOnlySequence<byte> _sequence;

    private JsonSegment(ReadOnlySequence<byte> sequence)
    {
        _sequence = sequence;
    }

    public bool IsEmpty => _sequence.IsEmpty;

    internal int Length => (int)_sequence.Length;

    /// <summary>
    /// Returns the bytes of this segment as a sequence over the memory that holds them. The
    /// sequence reads that memory and is valid for as long as it lives.
    /// </summary>
    public ReadOnlySequence<byte> AsSequence() => _sequence;

    public void WriteTo(JsonWriter writer)
    {
        if (_sequence.IsSingleSegment)
        {
            if (!_sequence.IsEmpty)
            {
                writer.WriteRawValue(_sequence.FirstSpan);
            }

            return;
        }

        var position = _sequence.Start;
        _sequence.TryGet(ref position, out var first);
        writer.WriteRawValueStart(first.Span);

        while (_sequence.TryGet(ref position, out var memory))
        {
            writer.WriteRawValueContinuation(memory.Span);
        }

        writer.WriteRawValueEnd();
    }

    public bool Equals(JsonSegment other)
    {
        var sequence = _sequence;
        var otherSequence = other._sequence;

        if (sequence.Length != otherSequence.Length)
        {
            return false;
        }

        if (sequence.IsSingleSegment && otherSequence.IsSingleSegment)
        {
            return sequence.FirstSpan.SequenceEqual(otherSequence.FirstSpan);
        }

        var reader = new SequenceReader<byte>(otherSequence);

        foreach (var memory in sequence)
        {
            if (!reader.IsNext(memory.Span, advancePast: true))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
        => obj is JsonSegment other && Equals(other);

    public override int GetHashCode()
    {
        uint hash;

        if (_sequence.IsSingleSegment)
        {
            hash = ChunkedArrayWriter.ComputeHashCore(0u, _sequence.FirstSpan);
        }
        else
        {
            hash = 0u;

            foreach (var memory in _sequence)
            {
                hash = ChunkedArrayWriter.ComputeHashCore(hash, memory.Span);
            }
        }

        return (int)(hash & 0x7FFFFFFF);
    }

    /// <summary>
    /// Copies the bytes of this segment into memory rented from <paramref name="arena"/>, so the
    /// returned segment stays valid when the memory of this segment is written again.
    /// </summary>
    internal JsonSegment Detach(IMemoryArena arena)
    {
        if (IsEmpty)
        {
            return this;
        }

        var remaining = _sequence;
        var first = CopyPiece(arena, ref remaining);

        if (remaining.IsEmpty)
        {
            return new JsonSegment(new ReadOnlySequence<byte>(first));
        }

        var firstSegment = new MemorySequenceSegment(first);
        var lastSegment = firstSegment;

        do
        {
            lastSegment = lastSegment.Append(CopyPiece(arena, ref remaining));
        }
        while (!remaining.IsEmpty);

        return new JsonSegment(
            new ReadOnlySequence<byte>(firstSegment, 0, lastSegment, lastSegment.Memory.Length));
    }

    public static JsonSegment Empty => default;

    internal static JsonSegment Create(ReadOnlySequence<byte> sequence) => new(sequence);

    // copies as many bytes as the arena hands out at once and moves remaining past them
    private static ReadOnlyMemory<byte> CopyPiece(IMemoryArena arena, ref ReadOnlySequence<byte> remaining)
    {
        var length = (int)Math.Min(remaining.Length, JsonMemory.BufferSize);
        var memory = arena.Rent(length).Memory;
        remaining.Slice(0, length).CopyTo(memory.Span);
        remaining = remaining.Slice(length);
        return memory;
    }
}
