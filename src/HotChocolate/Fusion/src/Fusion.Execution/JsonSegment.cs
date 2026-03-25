using HotChocolate.Buffers;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion;

public readonly struct JsonSegment : IEquatable<JsonSegment>
{
    private readonly ChunkedArrayWriter _memory;
    private readonly int _location;
    private readonly int _length;

    private JsonSegment(ChunkedArrayWriter memory, int location, int length)
    {
        _memory = memory;
        _location = location;
        _length = length;
    }

    public bool IsEmpty => _memory is null;

    public void WriteTo(JsonWriter writer)
    {
        if (IsEmpty)
        {
            return;
        }

        var start = _location;
        var length = _length;
        var first = _memory.Read(ref start, ref length);

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
            writer.WriteRawValueContinuation(_memory.Read(ref start, ref length));
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

        return _memory.SequenceEqual(_location, other._location, _length);
    }

    public override bool Equals(object? obj)
        => obj is JsonSegment other && Equals(other);

    public override int GetHashCode()
        => IsEmpty ? 0 : _memory.GetHashCode(_location, _length);

    public static JsonSegment Empty => default;

    internal static JsonSegment Create(ChunkedArrayWriter memory, int location, int length)
        => new(memory, location, length);
}
