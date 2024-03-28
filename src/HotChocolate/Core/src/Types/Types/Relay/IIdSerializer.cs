#nullable enable

using System;
using System.Buffers;
using System.Buffers.Text;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using HotChocolate.Language;

namespace HotChocolate.Types.Relay;

/// <summary>
/// The ID serializer is used to parse and format node ids.
/// </summary>
public interface IIdSerializer
{
    /// <summary>
    /// Creates a schema unique identifier from a source schema name,
    /// an ID and type name.
    /// </summary>
    /// <typeparam name="T">The id type.</typeparam>
    /// <param name="schemaName">The schema name.</param>
    /// <param name="typeName">The type name.</param>
    /// <param name="id">The id.</param>
    /// <returns>
    /// Returns an ID string containing the type name and the ID.
    /// </returns>
    /// <exception cref="IdSerializationException">
    /// Unable to create a schema unique ID string.
    /// </exception>
    string? Serialize<T>(string? schemaName, string typeName, T id);

    /// <summary>
    /// Deserializes a schema unique identifier to reveal the source
    /// schema, internal ID and type name of an object.
    /// </summary>
    /// <param name="serializedId">
    /// The schema unique ID string.
    /// </param>
    /// <returns>
    /// Returns an <see cref="IdValue"/> containing the information
    /// encoded into the unique ID string.
    /// </returns>
    /// <exception cref="IdSerializationException">
    /// Unable to deconstruct the schema unique ID string.
    /// </exception>
    IdValue Deserialize(string serializedId);
}

public sealed class StringNodeIdValueSerializer : INodeIdValueSerializer
{
    private readonly Encoding _utf8 = Encoding.UTF8;

    public NodeIdFormatterResult Format(Span<byte> buffer, object value, out int written)
    {
        if (value is string s)
        {
            var requiredCapacity = _utf8.GetByteCount(s);
            if (buffer.Length < requiredCapacity)
            {
                written = 0;
                return NodeIdFormatterResult.BufferTooSmall;
            }

            Utf8GraphQLParser.ConvertToBytes(s, ref buffer);
            written = buffer.Length;
            return NodeIdFormatterResult.Success;
        }

        written = 0;
        return NodeIdFormatterResult.InvalidValue;
    }

    public unsafe bool TryParse(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out object? value)
    {
        fixed (byte* b = buffer)
        {
            value = _utf8.GetString(b, buffer.Length);
            return true;
        }
    }
}

public sealed class Int16NodeIdValueSerializer : INodeIdValueSerializer
{
    public NodeIdFormatterResult Format(Span<byte> buffer, object value, out int written)
    {
        if (value is short i)
        {
            return Utf8Formatter.TryFormat(i, buffer, out written)
                ? NodeIdFormatterResult.Success
                : NodeIdFormatterResult.BufferTooSmall;
        }

        written = 0;
        return NodeIdFormatterResult.InvalidValue;
    }

    public unsafe bool TryParse(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out object? value)
    {
        if (Utf8Parser.TryParse(buffer, out short parsedValue, out _))
        {
            value = parsedValue;
            return true;
        }

        value = null;
        return false;
    }
}

public sealed class Int32NodeIdValueSerializer : INodeIdValueSerializer
{
    public NodeIdFormatterResult Format(Span<byte> buffer, object value, out int written)
    {
        if (value is int i)
        {
            return Utf8Formatter.TryFormat(i, buffer, out written)
                ? NodeIdFormatterResult.Success
                : NodeIdFormatterResult.BufferTooSmall;
        }

        written = 0;
        return NodeIdFormatterResult.InvalidValue;
    }

    public unsafe bool TryParse(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out object? value)
    {
        if (Utf8Parser.TryParse(buffer, out int parsedValue, out _))
        {
            value = parsedValue;
            return true;
        }

        value = null;
        return false;
    }
}

public sealed class Int64NodeIdValueSerializer : INodeIdValueSerializer
{
    public NodeIdFormatterResult Format(Span<byte> buffer, object value, out int written)
    {
        if (value is long i)
        {
            return Utf8Formatter.TryFormat(i, buffer, out written)
                ? NodeIdFormatterResult.Success
                : NodeIdFormatterResult.BufferTooSmall;
        }

        written = 0;
        return NodeIdFormatterResult.InvalidValue;
    }

    public unsafe bool TryParse(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out object? value)
    {
        if (Utf8Parser.TryParse(buffer, out long parsedValue, out _))
        {
            value = parsedValue;
            return true;
        }

        value = null;
        return false;
    }
}

public sealed class GuidNodeIdValueSerializer : INodeIdValueSerializer
{
    public NodeIdFormatterResult Format(Span<byte> buffer, object value, out int written)
    {
        if (value is Guid g)
        {
            return Utf8Formatter.TryFormat(g, buffer, out written)
                ? NodeIdFormatterResult.Success
                : NodeIdFormatterResult.BufferTooSmall;
        }

        written = 0;
        return NodeIdFormatterResult.InvalidValue;
    }

    public unsafe bool TryParse(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out object? value)
    {
        if (Utf8Parser.TryParse(buffer, out Guid parsedValue, out _))
        {
            value = parsedValue;
            return true;
        }

        value = null;
        return false;
    }
}

/// <summary>
/// The ID serializer is used to parse and format the value part if a node id.
/// </summary>
public interface INodeIdValueSerializer
{
    /// <summary>
    /// Formats the node id value into a byte buffer.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to write the formatted value into.
    /// </param>
    /// <param name="value">
    /// The value to format.
    /// </param>
    /// <param name="written">
    /// The number of bytes written to the buffer.
    /// </param>
    NodeIdFormatterResult Format(Span<byte> buffer, object value, out int written);

    /// <summary>
    /// Parses the node id value from a byte buffer.
    /// </summary>
    /// <param name="buffer">
    /// The byte buffer that contains the formatted id value.
    /// </param>
    /// <param name="value">
    /// The parsed value.
    /// </param>
    /// <returns>
    /// Returns true if the value could be parsed.
    /// </returns>
    bool TryParse(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out object? value);
}

public enum NodeIdFormatterResult
{
    Success,
    BufferTooSmall,
    InvalidValue,
}

public interface INodeIdSerializer
{
    string Format(string typeName, object internalId);

    NodeId Parse(string formattedId);
}

public readonly struct NodeId(string typeName, object internalId)
{
    public string TypeName { get; } = typeName;

    public object InternalId { get; } = internalId;

    public bool Equals(NodeId other)
        => TypeName == other.TypeName &&
            InternalId.Equals(other.InternalId);

    public override bool Equals(object? obj)
        => obj is NodeId other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(TypeName, InternalId);

    public override string ToString()
        => $"{TypeName}:{InternalId}";

    public static bool operator ==(NodeId left, NodeId right)
        => left.Equals(right);

    public static bool operator !=(NodeId left, NodeId right)
        => !left.Equals(right);
}

public class DefaultNodeIdSerializer : INodeIdSerializer
{
    private const byte _delimiter = (byte)':';
    private const int _stackallocThreshold = 256;
    private static readonly Encoding _utf8 = Encoding.UTF8;

#if NET8_0_OR_GREATER
    private readonly FrozenDictionary<string, Serializer> _stringSerializerMap;
#else
    private readonly Dictionary<string, Serializer> _stringSerializerMap;
#endif
    private readonly SpanSerializerMap _spanSerializerMap;

    public DefaultNodeIdSerializer(IEnumerable<NodeIdSerializerEntry> serializers)
    {
#if NET8_0_OR_GREATER
        _stringSerializerMap =
 serializers.ToFrozenDictionary(t => t.TypeName, t => new Serializer(t.TypeName, t.Serializer));
#else
        _stringSerializerMap = serializers.ToDictionary(t => t.TypeName, t => new Serializer(t.TypeName, t.Serializer));
#endif
        _spanSerializerMap = new SpanSerializerMap();
        foreach (var serializer in _stringSerializerMap.Values)
        {
            _spanSerializerMap.Add(serializer.FormattedTypeName, serializer);
        }
    }

    public string Format(string typeName, object internalId)
    {
        if (typeName is null)
        {
            throw new ArgumentNullException(nameof(typeName));
        }

        if (internalId is null)
        {
            throw new ArgumentNullException(nameof(internalId));
        }

        if (!_stringSerializerMap.TryGetValue(typeName, out var serializer))
        {
            throw new NodeIdMissingSerializerException(typeName);
        }

        return serializer.Format(internalId);
    }

    public NodeId Parse(string formattedId)
    {
        if (formattedId is null)
        {
            throw new ArgumentNullException(nameof(formattedId));
        }

        var expectedSize = _utf8.GetByteCount(formattedId);

        byte[]? rentedBuffer = null;
        var span = expectedSize <= _stackallocThreshold
            ? stackalloc byte[_stackallocThreshold]
            : rentedBuffer = ArrayPool<byte>.Shared.Rent(expectedSize);

        Utf8GraphQLParser.ConvertToBytes(formattedId, ref span);
        var index = span.IndexOf(_delimiter);
        var typeName = span.Slice(0, index);

        if (!_spanSerializerMap.TryGetValue(typeName, out var serializer))
        {
            Clear(rentedBuffer);
            throw new InvalidOperationException("TypeResources.DefaultNodeIdSerializer_NoSerializerFound");
        }

        var value = serializer.Parse(span.Slice(index + 1));
        Clear(rentedBuffer);
        return new NodeId(serializer.TypeName, value);

        static void Clear(byte[]? rentedBuffer = null)
        {
            if (rentedBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }
    }

    private sealed class Serializer(string typeName, INodeIdValueSerializer valueSerializer)
    {
        private readonly byte[] _formattedTypeName = _utf8.GetBytes(typeName);

        public string TypeName => typeName;

        public byte[] FormattedTypeName => _formattedTypeName;

        public unsafe string Format(object value)
        {
            var minLength = _formattedTypeName.Length + 128;
            byte[]? rentedBuffer = null;
            var span = minLength <= _stackallocThreshold
                ? stackalloc byte[_stackallocThreshold]
                : rentedBuffer = ArrayPool<byte>.Shared.Rent(minLength);
            var capacity = span.Length;

            _formattedTypeName.CopyTo(span);
            var valueSpan = span.Slice(_formattedTypeName.Length);
            valueSpan[0] = _delimiter;
            valueSpan = valueSpan.Slice(1);
            var result = valueSerializer.Format(valueSpan, value, out var written);

            while (result == NodeIdFormatterResult.BufferTooSmall)
            {
                if (rentedBuffer is not null)
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                }

                capacity *= 2;
                rentedBuffer = ArrayPool<byte>.Shared.Rent(capacity);
                valueSpan = rentedBuffer;

                _formattedTypeName.CopyTo(span);
                valueSpan = valueSpan.Slice(_formattedTypeName.Length);
                valueSpan[0] = _delimiter;
                valueSpan = valueSpan.Slice(1);
                result = valueSerializer.Format(valueSpan, value, out written);
            }

            if (result == NodeIdFormatterResult.Success)
            {
                fixed (byte* buffer = rentedBuffer)
                {
                    Clear(rentedBuffer);
                    return _utf8.GetString(buffer, written);
                }
            }

            Clear(rentedBuffer);

            throw new NodeIdInvalidFormatException(value);

            static void Clear(byte[]? rentedBuffer = null)
            {
                if (rentedBuffer is not null)
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                }
            }
        }

        public unsafe NodeId Parse(ReadOnlySpan<byte> formattedValue)
        {
            if (valueSerializer.TryParse(formattedValue, out var internalId))
            {
                return new NodeId(typeName, internalId);
            }

            fixed(byte* buffer = formattedValue)
            {
                throw new NodeIdInvalidFormatException(_utf8.GetString(buffer, formattedValue.Length));
            }
        }
    }

    private sealed class SpanSerializerMap(int size = 100)
    {
        private const int _initialBucketSize = 4;
        private readonly Entry[]?[] _buckets = new Entry[size][];

        public void Add(byte[] formattedTypeName, Serializer serializer)
        {
            var hashCode = GetSpanHashCode(formattedTypeName);
            var bucketIndex = Math.Abs(hashCode % _buckets.Length);
            var bucket = _buckets[bucketIndex];

            if (bucket == null)
            {
                bucket = new Entry[_initialBucketSize];
                _buckets[bucketIndex] = bucket;
            }

            var foundSpot = false;
            var bucketSize = bucket.Length;
            var insertAt = 0;
            for (; insertAt < bucketSize; insertAt++)
            {
                var entry = bucket[insertAt];
                if (entry.Key == null!)
                {
                    foundSpot = true;
                    break;
                }

                if (entry.HashCode == hashCode && entry.Key.AsSpan().SequenceEqual(formattedTypeName))
                {
                    throw new ArgumentException("An item with the same key has already been added.");
                }
            }

            if (!foundSpot)
            {
                var newBucket = new Entry[bucket.Length * 2];
                _buckets[bucketIndex] = newBucket;
                Array.Copy(bucket, newBucket, bucket.Length);
                insertAt = _buckets.Length;
                bucket = newBucket;
            }

            bucket[insertAt] = new Entry { HashCode = hashCode, Key = formattedTypeName, Value = serializer, };
        }

        public bool TryGetValue(
            ReadOnlySpan<byte> formattedTypeName,
            [NotNullWhen(true)] out Serializer? serializer)
        {
            var hashCode = GetSpanHashCode(formattedTypeName);
            var bucketIndex = Math.Abs(hashCode % _buckets.Length);
            var bucket = _buckets[bucketIndex];

            if (bucket == null)
            {
                serializer = null;
                return false;
            }

            ref var entry = ref MemoryMarshal.GetReference(bucket.AsSpan());
            ref var end = ref Unsafe.Add(ref entry, bucket.Length);
            while (Unsafe.IsAddressLessThan(ref entry, ref end))
            {
                if (entry.Key == null!)
                {
                    // we have reached end of entries in this bucket
                    break;
                }

                if (entry.HashCode == hashCode &&
                    formattedTypeName.SequenceEqual(entry.Key))
                {
                    serializer = entry.Value;
                    return true;
                }

                entry = ref Unsafe.Add(ref entry, 1);
            }

            serializer = default;
            return false;
        }

        private static int GetSpanHashCode(ReadOnlySpan<byte> span)
        {
            unchecked
            {
                var hash = 17;
                foreach (var b in span)
                {
                    hash = hash * 31 + b;
                }

                return hash;
            }
        }

        private struct Entry
        {
            public int HashCode;
            public byte[] Key;
            public Serializer Value;
        }
    }
}

public sealed class NodeIdSerializerEntry(
    string typeName,
    INodeIdValueSerializer serializer)
    : IEquatable<NodeIdSerializerEntry>
{
    public string TypeName { get; } = typeName;

    public INodeIdValueSerializer Serializer {get;} = serializer;

    public bool Equals(NodeIdSerializerEntry? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return TypeName == other.TypeName &&
            Serializer.Equals(other.Serializer);
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || obj is NodeIdSerializerEntry other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(TypeName, Serializer);

    public static bool operator ==(NodeIdSerializerEntry? left, NodeIdSerializerEntry? right)
        => Equals(left, right);

    public static bool operator !=(NodeIdSerializerEntry? left, NodeIdSerializerEntry? right)
        => !Equals(left, right);
}

public sealed class NodeIdInvalidFormatException(object originalValue)
    : GraphQLException(ErrorBuilder.New()
        .SetMessage("The internal ID could not be formatted.")
        .SetExtension(nameof(originalValue), originalValue.ToString())
        .Build());

public sealed class NodeIdMissingSerializerException(string typeName)
    : GraphQLException(ErrorBuilder.New()
        .SetMessage("No serializer registered for type `{0}`.", typeName)
        .SetExtension(nameof(typeName), typeName)
        .Build());
