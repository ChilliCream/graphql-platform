#nullable enable
using System;
using System.Buffers;
using System.Buffers.Text;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#else
using System.Linq;
#endif
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using HotChocolate.Language;

namespace HotChocolate.Types.Relay;

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

    public unsafe NodeId Parse(string formattedId)
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
        Base64.DecodeFromUtf8InPlace(span, out var written);
        span = span.Slice(0, written);

        var index = span.IndexOf(_delimiter);
        if(index == -1)
        {
            Clear(rentedBuffer);
            throw new NodeIdInvalidFormatException(formattedId);
        }

        var typeName = span.Slice(0, index);
        if (!_spanSerializerMap.TryGetValue(typeName, out var serializer))
        {
            string typeNameString;
            fixed(byte* typeNamePtr = typeName)
            {
                typeNameString = _utf8.GetString(typeNamePtr, typeName.Length);
            }

            Clear(rentedBuffer);
            throw new NodeIdMissingSerializerException(typeNameString);
        }

        var valueSpan = span.Slice(index + 1);
        if(valueSpan.IsEmpty)
        {
            Clear(rentedBuffer);
            throw new NodeIdInvalidFormatException(formattedId);
        }

        var value = serializer.Parse(valueSpan);
        Clear(rentedBuffer);
        return value;

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
                capacity *= 2;
                var newBuffer = ArrayPool<byte>.Shared.Rent(capacity);
                span = newBuffer;
                capacity = newBuffer.Length;

                if (rentedBuffer is not null)
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                }
                rentedBuffer = newBuffer;

                _formattedTypeName.CopyTo(span);
                valueSpan = span.Slice(_formattedTypeName.Length);
                valueSpan[0] = _delimiter;
                valueSpan = valueSpan.Slice(1);
                result = valueSerializer.Format(valueSpan, value, out written);
            }

            if (result == NodeIdFormatterResult.Success)
            {
                string? formattedId;
                var dataLength = _formattedTypeName.Length + 1 + written;

                while (Base64.EncodeToUtf8InPlace(span, dataLength, out written) == OperationStatus.DestinationTooSmall)
                {
                    capacity *= 2;
                    var newBuffer = ArrayPool<byte>.Shared.Rent(capacity);
                    span.Slice(0, dataLength).CopyTo(newBuffer);
                    span = newBuffer;
                    capacity = newBuffer.Length;

                    if (rentedBuffer is not null)
                    {
                        ArrayPool<byte>.Shared.Return(rentedBuffer);
                    }

                    rentedBuffer = newBuffer;
                }

                fixed (byte* buffer = span)
                {
                    formattedId = _utf8.GetString(buffer, written);
                }

                Clear(rentedBuffer);
                return formattedId;
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

            fixed (byte* buffer = formattedValue)
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
