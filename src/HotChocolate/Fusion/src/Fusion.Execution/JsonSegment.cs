using System.Buffers;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion;

public readonly struct JsonSegment : IEquatable<JsonSegment>
{
    private readonly IJsonSegmentSource? _memory;
    private readonly int _location;
    private readonly int _length;

    private JsonSegment(IJsonSegmentSource memory, int location, int length)
    {
        _memory = memory;
        _location = location;
        _length = length;
    }

    public bool IsEmpty => _memory is null;

    internal int Location => _location;

    internal int Length => _length;

    public ReadOnlySequence<byte> AsSequence()
    {
        if (IsEmpty)
        {
            return ReadOnlySequence<byte>.Empty;
        }

        var start = _location;
        var length = _length;
        var first = Read(ref start, ref length);

        if (length == 0)
        {
            // Single chunk, common case with no allocation for segment chain.
            return new ReadOnlySequence<byte>(first.ToArray());
        }

        // Multi-chunk, build a ReadOnlySequence from linked segments.
        var firstSegment = new MemorySegment(first.ToArray());
        var lastSegment = firstSegment;

        do
        {
            lastSegment = lastSegment.Append(Read(ref start, ref length));
        }
        while (length > 0);

        return new ReadOnlySequence<byte>(firstSegment, 0, lastSegment, lastSegment.Memory.Length);
    }

    public void WriteTo(JsonWriter writer)
    {
        if (IsEmpty)
        {
            return;
        }

        var start = _location;
        var length = _length;
        var first = Read(ref start, ref length);

        if (length == 0)
        {
            // Single chunk, common case.
            writer.WriteRawValue(first);
            return;
        }

        // Multi-chunk, write start, continuations, then set separator flag.
        writer.WriteRawValueStart(first);

        do
        {
            writer.WriteRawValueContinuation(Read(ref start, ref length));
        }
        while (length > 0);

        writer.WriteRawValueEnd();
    }

    public bool Equals(JsonSegment other)
    {
        if (IsEmpty)
        {
            return other.IsEmpty;
        }

        if (other.IsEmpty)
        {
            return false;
        }

        return ReferenceEquals(_memory, other._memory)
            && _memory!.SequenceEqual(_location, other._location, _length);
    }

    public override bool Equals(object? obj)
        => obj is JsonSegment other && Equals(other);

    public override int GetHashCode()
        => IsEmpty
            ? 0
            : _memory!.GetHashCode(_location, _length);

    public static JsonSegment Empty => default;

    internal static JsonSegment Create(ChunkedArrayWriter memory, int location, int length)
        => new(memory, location, length);

    internal static JsonSegment Create(ArenaBufferWriter memory, int location, int length)
        => new(memory, location, length);

    private ReadOnlySpan<byte> Read(ref int start, ref int length)
        => _memory!.Read(ref start, ref length);

    private sealed class MemorySegment : ReadOnlySequenceSegment<byte>
    {
        public MemorySegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public MemorySegment Append(ReadOnlySpan<byte> data)
        {
            var next = new MemorySegment(data.ToArray())
            {
                RunningIndex = RunningIndex + Memory.Length
            };
            Next = next;
            return next;
        }
    }
}
