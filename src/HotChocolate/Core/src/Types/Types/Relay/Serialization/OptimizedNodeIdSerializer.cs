#nullable enable
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using HotChocolate.Language;

namespace HotChocolate.Types.Relay;

internal sealed class OptimizedNodeIdSerializer : INodeIdSerializer
{
    private const byte _delimiter = (byte)':';
    private const byte _legacyDelimiter = (byte)'\n';
    private const int _stackallocThreshold = 256;
    private static readonly Encoding _utf8 = Encoding.UTF8;

    private readonly FrozenDictionary<string, Serializer> _stringSerializerMap;
    private readonly SpanSerializerMap _spanSerializerMap;
    private readonly INodeIdValueSerializer[] _serializers;
    private readonly int _maxIdLength;
    private readonly bool _outputNewIdFormat;
    private readonly bool _urlSafeBase64;

    internal OptimizedNodeIdSerializer(
        IEnumerable<BoundNodeIdValueSerializer> boundSerializers,
        INodeIdValueSerializer[] allSerializers,
        int maxIdLength = 1024,
        bool outputNewIdFormat = true,
        bool urlSafeBase64 = true)
    {
        _stringSerializerMap =
            boundSerializers.ToFrozenDictionary(
                t => t.TypeName,
                t => new Serializer(t.TypeName, t.Serializer, outputNewIdFormat, urlSafeBase64));

        _serializers = allSerializers;
        _spanSerializerMap = new SpanSerializerMap();
        foreach (var serializer in _stringSerializerMap.Values)
        {
            _spanSerializerMap.Add(serializer.FormattedTypeName, serializer);
        }

        _maxIdLength = maxIdLength;
        _outputNewIdFormat = outputNewIdFormat;
        _urlSafeBase64 = urlSafeBase64;
    }

    public string Format(string typeName, object internalId)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            throw new ArgumentException(
                "Value cannot be null or empty.",
                nameof(typeName));
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

    public unsafe NodeId Parse(string formattedId, INodeIdRuntimeTypeLookup runtimeTypeLookup)
    {
        if (formattedId is null)
        {
            throw new ArgumentNullException(nameof(formattedId));
        }

        if (formattedId.Length > _maxIdLength)
        {
            throw new NodeIdInvalidFormatException(formattedId);
        }

        var expectedSize = _utf8.GetByteCount(formattedId);

        byte[]? rentedBuffer = null;
        var span = expectedSize <= _stackallocThreshold
            ? stackalloc byte[_stackallocThreshold]
            : rentedBuffer = ArrayPool<byte>.Shared.Rent(expectedSize);

        Utf8GraphQLParser.ConvertToBytes(formattedId, ref span);

        if (_urlSafeBase64)
        {
            for(var i = 0; i < span.Length; i++)
            {
                if (span[i] == (byte)'-')
                {
                    span[i] = (byte)'+';
                }
                else if (span[i] == (byte)'_')
                {
                    span[i] = (byte)'/';
                }
            }
        }

        Base64.DecodeFromUtf8InPlace(span, out var written);
        span = span.Slice(0, written);

        var delimiterIndex = FindDelimiterIndex(span);
        if (delimiterIndex == -1)
        {
            Clear(rentedBuffer);
            throw new NodeIdInvalidFormatException(formattedId);
        }

        var delimiterOffset = 1;
        if (span[delimiterIndex] == _legacyDelimiter)
        {
            delimiterOffset = 2;
        }

        var typeName = span.Slice(0, delimiterIndex);
        if (!_spanSerializerMap.TryGetValue(typeName, out var serializer))
        {
            var typeNameString = ToString(typeName);
            Clear(rentedBuffer);
            throw new NodeIdMissingSerializerException(typeNameString);
        }

        var value = serializer.Parse(span.Slice(delimiterIndex + delimiterOffset));
        Clear(rentedBuffer);
        return value;
    }

    public unsafe NodeId Parse(string formattedId, Type runtimeType)
    {
        if (formattedId is null)
        {
            throw new ArgumentNullException(nameof(formattedId));
        }

        if (runtimeType is null)
        {
            throw new ArgumentNullException(nameof(runtimeType));
        }

        if (formattedId.Length > _maxIdLength)
        {
            throw new NodeIdInvalidFormatException(formattedId);
        }

        var expectedSize = _utf8.GetByteCount(formattedId);

        byte[]? rentedBuffer = null;
        var span = expectedSize <= _stackallocThreshold
            ? stackalloc byte[_stackallocThreshold]
            : rentedBuffer = ArrayPool<byte>.Shared.Rent(expectedSize);

        Utf8GraphQLParser.ConvertToBytes(formattedId, ref span);

        if (_urlSafeBase64)
        {
            for(var i = 0; i < span.Length; i++)
            {
                if (span[i] == (byte)'-')
                {
                    span[i] = (byte)'+';
                }
                else if (span[i] == (byte)'_')
                {
                    span[i] = (byte)'/';
                }
            }
        }

        Base64.DecodeFromUtf8InPlace(span, out var written);
        span = span.Slice(0, written);

        var delimiterIndex = FindDelimiterIndex(span);
        if (delimiterIndex == -1)
        {
            Clear(rentedBuffer);
            throw new NodeIdInvalidFormatException(formattedId);
        }

        var delimiterOffset = 1;
        if (span[delimiterIndex] == _legacyDelimiter)
        {
            delimiterOffset = 2;
        }

        var typeName = span.Slice(0, delimiterIndex);
        INodeIdValueSerializer? valueSerializer = null;
        if (!_spanSerializerMap.TryGetValue(typeName, out var serializer))
        {
            valueSerializer = TryResolveSerializer(runtimeType);

            if (valueSerializer is null)
            {
                throw SerializerMissing(typeName, rentedBuffer);
            }

            lock (_spanSerializerMap)
            {
                if (!_spanSerializerMap.TryGetValue(typeName, out serializer))
                {
                    serializer = new Serializer(
                        ToString(typeName),
                        valueSerializer,
                        _outputNewIdFormat,
                        _urlSafeBase64);
                    _spanSerializerMap.Add(serializer.FormattedTypeName, serializer);
                }
            }
        }

        if (serializer.ValueSerializer.IsSupported(runtimeType))
        {
            valueSerializer = serializer.ValueSerializer;
        }
        else if (valueSerializer is null)
        {
            valueSerializer = TryResolveSerializer(runtimeType);
        }

        if (valueSerializer is null)
        {
            throw SerializerMissing(typeName, rentedBuffer);
        }

        var value = ParseValue(valueSerializer, serializer.TypeName, span.Slice(delimiterIndex + delimiterOffset));
        Clear(rentedBuffer);
        return value;
    }

    private INodeIdValueSerializer? TryResolveSerializer(Type type)
    {
        ref var serializer = ref MemoryMarshal.GetReference(_serializers.AsSpan());
        ref var end = ref Unsafe.Add(ref serializer, _serializers.Length);

        while (Unsafe.IsAddressLessThan(ref serializer, ref end))
        {
            if (serializer.IsSupported(type))
            {
                return serializer;
            }

            serializer = ref Unsafe.Add(ref serializer, 1)!;
        }

        return null;
    }

    private static NodeId ParseValue(
        INodeIdValueSerializer valueSerializer,
        string typeName,
        ReadOnlySpan<byte> formattedValue)
    {
        if (valueSerializer.TryParse(formattedValue, out var internalId))
        {
            return new NodeId(typeName, internalId);
        }

        throw new NodeIdInvalidFormatException(ToString(formattedValue));
    }

    private static readonly byte[] _delimiters = [_delimiter, _legacyDelimiter];
    private static readonly SearchValues<byte>
        _delimiterSearchValues = SearchValues.Create(_delimiters);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindDelimiterIndex(ReadOnlySpan<byte> span)
    {
        return span.IndexOfAny(_delimiterSearchValues);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe string ToString(ReadOnlySpan<byte> span)
    {
        fixed (byte* buffer = span)
        {
            return _utf8.GetString(buffer, span.Length);
        }
    }

    private static NodeIdMissingSerializerException SerializerMissing(
        ReadOnlySpan<byte> typeName,
        byte[]? rentedBuffer = null)
    {
        var typeNameString = ToString(typeName);
        Clear(rentedBuffer);
        return new NodeIdMissingSerializerException(typeNameString);
    }

    private static void Clear(byte[]? rentedBuffer = null)
    {
        if (rentedBuffer is not null)
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

    private sealed class Serializer(
        string typeName,
        INodeIdValueSerializer valueSerializer,
        bool outputNewIdFormat,
        bool urlSafeBase64)
    {
        private readonly byte[] _formattedTypeName = _utf8.GetBytes(typeName);

        public string TypeName => typeName;

        public byte[] FormattedTypeName => _formattedTypeName;

        public INodeIdValueSerializer ValueSerializer => valueSerializer;

        public unsafe string Format(object value)
        {
            var minLength = _formattedTypeName.Length + 128;
            byte[]? rentedBuffer = null;
            var span = minLength <= _stackallocThreshold
                ? stackalloc byte[_stackallocThreshold]
                : rentedBuffer = ArrayPool<byte>.Shared.Rent(minLength);
            var capacity = span.Length;

            var valueSpan = WriteIdHeader(span, _formattedTypeName, value, outputNewIdFormat);

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

                valueSpan = WriteIdHeader(span, _formattedTypeName, value, outputNewIdFormat);

                result = valueSerializer.Format(valueSpan, value, out written);
            }

            if (result == NodeIdFormatterResult.Success)
            {
                var delimiterLength = outputNewIdFormat ? 1 : 2;
                var dataLength = _formattedTypeName.Length + delimiterLength + written;

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

                span = span.Slice(0, written);

                if (urlSafeBase64)
                {
                    for (var i = 0; i < span.Length; i++)
                    {
                        if (span[i] == '+')
                        {
                            span[i] = (byte)'-';
                        }
                        else if (span[i] == '/')
                        {
                            span[i] = (byte)'_';
                        }
                    }
                }

                var formattedId = OptimizedNodeIdSerializer.ToString(span);

                Clear(rentedBuffer);
                return formattedId;
            }

            Clear(rentedBuffer);

            throw new NodeIdInvalidFormatException(value);
        }

        public NodeId Parse(ReadOnlySpan<byte> formattedValue)
            => ParseValue(valueSerializer, typeName, formattedValue);

        private static Span<byte> WriteIdHeader(
            Span<byte> span,
            ReadOnlySpan<byte> typeName,
            object value,
            bool outputNewIdFormat)
        {
            typeName.CopyTo(span);

            var valueSpan = span.Slice(typeName.Length);

            if (outputNewIdFormat)
            {
                valueSpan[0] = _delimiter;
                return valueSpan.Slice(1);
            }

            valueSpan[0] = _legacyDelimiter;
            valueSpan[1] = LegacyNodeIdSerializer.GetLegacyValueCode(value);
            return valueSpan.Slice(2);
        }
    }

    // we keep the initial bucket size small to reduce memory overhead since we usually will build the map
    // once and then only read from it. Even in the case where we add serializers at runtime, the overhead
    // of resizing the buckets is not that big as the map will become constant after a while since all types
    // are known.
    private sealed class SpanSerializerMap(int size = 100, int initialBucketSize = 1)
    {
        private readonly Entry[]?[] _buckets = new Entry[size][];

        public void Add(byte[] formattedTypeName, Serializer serializer)
        {
            var hashCode = GetSpanHashCode(formattedTypeName);
            var bucketIndex = Math.Abs(hashCode % _buckets.Length);
            var bucket = _buckets[bucketIndex];

            if (bucket == null)
            {
                bucket = new Entry[initialBucketSize];
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
                // we will only resize the bucket if we have not found a spot, and we will only add one additional
                // spot to the bucket. This is to reduce memory overhead, even as there is a memory overhead
                // when we have to resize the bucket.
                var requiredCapacity = bucket.Length + 1;
                var newBucket = new Entry[requiredCapacity];
                _buckets[bucketIndex] = newBucket;
                Array.Copy(bucket, newBucket, bucket.Length);
                insertAt = bucket.Length;
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

                if (entry.HashCode == hashCode && formattedTypeName.SequenceEqual(entry.Key))
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
