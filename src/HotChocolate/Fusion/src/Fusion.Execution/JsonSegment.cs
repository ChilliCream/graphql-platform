using System.Buffers;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion;

public readonly struct JsonSegment : IEquatable<JsonSegment>
{
    // a ChunkedArrayWriter, an ArenaBufferWriter or a byte array
    private readonly object? _memory;
    private readonly int _location;
    private readonly int _length;

    private JsonSegment(object memory, int location, int length)
    {
        _memory = memory;
        _location = location;
        _length = length;
    }

    public bool IsEmpty => _memory is null;

    internal int Location => _location;

    internal int Length => _length;

    /// <summary>
    /// Returns the bytes of this segment as a sequence over the memory that holds them. The
    /// sequence reads that memory and is valid for as long as it lives.
    /// </summary>
    public ReadOnlySequence<byte> AsSequence()
    {
        if (IsEmpty)
        {
            return ReadOnlySequence<byte>.Empty;
        }

        var start = _location;
        var length = _length;
        var first = ReadMemory(ref start, ref length);

        if (length == 0)
        {
            // Single chunk — common case, no allocation for segment chain.
            return new ReadOnlySequence<byte>(first);
        }

        // Multi-chunk — build a ReadOnlySequence from linked segments.
        var firstSegment = new MemorySegment(first);
        var lastSegment = firstSegment;

        do
        {
            lastSegment = lastSegment.Append(ReadMemory(ref start, ref length));
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
            // Single chunk — common case.
            writer.WriteRawValue(first);
            return;
        }

        // Multi-chunk — write start, continuations, then set separator flag.
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

        if (other.IsEmpty || !ReferenceEquals(_memory, other._memory))
        {
            return false;
        }

        switch (_memory)
        {
            case ArenaBufferWriter arena:
                return arena.SequenceEqual(_location, other._location, _length);

            case ChunkedArrayWriter chunked:
                return chunked.SequenceEqual(_location, other._location, _length);

            default:
                var bytes = (byte[])_memory!;
                return bytes.AsSpan(_location, _length).SequenceEqual(bytes.AsSpan(other._location, _length));
        }
    }

    public override bool Equals(object? obj)
        => obj is JsonSegment other && Equals(other);

    public override int GetHashCode()
        => _memory switch
        {
            null => 0,
            ArenaBufferWriter arena => arena.GetHashCode(_location, _length),
            ChunkedArrayWriter chunked => chunked.GetHashCode(_location, _length),
            _ => (int)(ChunkedArrayWriter.ComputeHashCore(0u, ((byte[])_memory).AsSpan(_location, _length))
                & 0x7FFFFFFF)
        };

    /// <summary>
    /// Copies the bytes of this segment into memory that only the returned segment reads, so the
    /// returned segment stays valid when the memory of this segment is reused.
    /// </summary>
    internal JsonSegment Detach()
    {
        if (IsEmpty)
        {
            return this;
        }

        var bytes = new byte[_length];
        var destination = bytes.AsSpan();
        var start = _location;
        var length = _length;

        while (length > 0)
        {
            var source = Read(ref start, ref length);
            source.CopyTo(destination);
            destination = destination[source.Length..];
        }

        return new JsonSegment(bytes, 0, bytes.Length);
    }

    public static JsonSegment Empty => default;

    internal static JsonSegment Create(ChunkedArrayWriter memory, int location, int length)
        => new(memory, location, length);

    internal static JsonSegment Create(ArenaBufferWriter memory, int location, int length)
        => new(memory, location, length);

    private ReadOnlySpan<byte> Read(ref int start, ref int length)
        => _memory switch
        {
            ArenaBufferWriter arena => arena.Read(ref start, ref length),
            ChunkedArrayWriter chunked => chunked.Read(ref start, ref length),
            _ => ReadMemory(ref start, ref length).Span
        };

    private ReadOnlyMemory<byte> ReadMemory(ref int start, ref int length)
    {
        switch (_memory)
        {
            case ArenaBufferWriter arena:
                return arena.ReadMemory(ref start, ref length);

            case ChunkedArrayWriter chunked:
                return chunked.ReadMemory(ref start, ref length);

            default:
                var bytes = ((byte[])_memory!).AsMemory(start, length);
                start += length;
                length = 0;
                return bytes;
        }
    }

    private sealed class MemorySegment : ReadOnlySequenceSegment<byte>
    {
        public MemorySegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public MemorySegment Append(ReadOnlyMemory<byte> data)
        {
            var next = new MemorySegment(data)
            {
                RunningIndex = RunningIndex + Memory.Length
            };
            Next = next;
            return next;
        }
    }
}
