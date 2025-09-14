using System.Buffers;
using System.Buffers.Text;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Buffers.Text;

namespace HotChocolate.Types.Relay;

internal sealed class OptimizedNodeIdSerializer : INodeIdSerializer
{
    private const byte Delimiter = (byte)':';
    private const byte LegacyDelimiter = (byte)'\n';
    private const int StackallocThreshold = 256;
    private static readonly Encoding s_utf8 = Encoding.UTF8;

    private readonly FrozenDictionary<string, Serializer> _stringSerializerMap;
    private readonly SpanSerializerMap _spanSerializerMap;
    private readonly INodeIdValueSerializer[] _serializers;
    private readonly int _maxIdLength;
    private readonly bool _outputNewIdFormat;
    private readonly NodeIdSerializerFormat _format;

    internal OptimizedNodeIdSerializer(
        IEnumerable<BoundNodeIdValueSerializer> boundSerializers,
        INodeIdValueSerializer[] allSerializers,
        int maxIdLength = 1024,
        bool outputNewIdFormat = true,
        NodeIdSerializerFormat format = NodeIdSerializerFormat.Base64)
    {
        _stringSerializerMap =
            boundSerializers.ToFrozenDictionary(
                t => t.TypeName,
                t => new Serializer(t.TypeName, t.Serializer, outputNewIdFormat, format));

        _serializers = allSerializers;
        _spanSerializerMap = new SpanSerializerMap();
        foreach (var serializer in _stringSerializerMap.Values)
        {
            _spanSerializerMap.Add(serializer.FormattedTypeName, serializer);
        }

        _maxIdLength = maxIdLength;
        _outputNewIdFormat = outputNewIdFormat;
        _format = format;
    }

    public string Format(string typeName, object internalId)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);
        ArgumentNullException.ThrowIfNull(internalId);

        if (!_stringSerializerMap.TryGetValue(typeName, out var serializer))
        {
            throw new NodeIdMissingSerializerException(typeName);
        }

        return serializer.Format(internalId);
    }

    public NodeId Parse(string formattedId, INodeIdRuntimeTypeLookup runtimeTypeLookup)
    {
        ArgumentNullException.ThrowIfNull(formattedId);

        if (formattedId.Length > _maxIdLength)
        {
            throw new NodeIdInvalidFormatException(formattedId);
        }

        return _format switch
        {
            NodeIdSerializerFormat.Base64 or NodeIdSerializerFormat.UrlSafeBase64
                => ParseBase64(formattedId, runtimeTypeLookup.GetNodeIdRuntimeType),
            NodeIdSerializerFormat.UpperHex or NodeIdSerializerFormat.LowerHex
                => ParseHex(formattedId, runtimeTypeLookup.GetNodeIdRuntimeType),
            NodeIdSerializerFormat.Base36
                => ParseBase36(formattedId, runtimeTypeLookup.GetNodeIdRuntimeType),
            _ => throw new NotSupportedException("Unsupported format.")
        };
    }

    public NodeId Parse(string formattedId, Type runtimeType)
    {
        ArgumentNullException.ThrowIfNull(formattedId);
        ArgumentNullException.ThrowIfNull(runtimeType);

        if (formattedId.Length > _maxIdLength)
        {
            throw new NodeIdInvalidFormatException(formattedId);
        }

        return _format switch
        {
            NodeIdSerializerFormat.Base64 or NodeIdSerializerFormat.UrlSafeBase64
                => ParseBase64(formattedId, _ => runtimeType),
            NodeIdSerializerFormat.UpperHex or NodeIdSerializerFormat.LowerHex
                => ParseHex(formattedId, _ => runtimeType),
            NodeIdSerializerFormat.Base36
                => ParseBase36(formattedId, _ => runtimeType),
            _ => throw new NotSupportedException("Unsupported format.")
        };
    }

    private NodeId ParseBase64(string formattedId, Func<string, Type?> getType)
    {
        var expectedSize = s_utf8.GetByteCount(formattedId);

        byte[]? rentedBuffer = null;
        var span = expectedSize <= StackallocThreshold
            ? stackalloc byte[StackallocThreshold]
            : rentedBuffer = ArrayPool<byte>.Shared.Rent(expectedSize);

        try
        {
            Utf8GraphQLParser.ConvertToBytes(formattedId, ref span);

            if (_format == NodeIdSerializerFormat.UrlSafeBase64)
            {
                for (var i = 0; i < span.Length; i++)
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

            // Ensure correct padding.
            var firstPaddingIndex = span.IndexOf((byte)'=');
            var nonPaddedLength = firstPaddingIndex == -1 ? span.Length : firstPaddingIndex;
            var actualPadding = firstPaddingIndex == -1 ? 0 : span.Length - firstPaddingIndex;
            var expectedPadding = (4 - nonPaddedLength % 4) % 4;

            if (actualPadding != expectedPadding)
            {
                Span<byte> correctedSpan = stackalloc byte[nonPaddedLength + expectedPadding];
                span[..nonPaddedLength].CopyTo(correctedSpan);

                for (var i = nonPaddedLength; i < correctedSpan.Length; i++)
                {
                    correctedSpan[i] = (byte)'=';
                }

                span = correctedSpan;
            }

            var operationStatus = Base64.DecodeFromUtf8InPlace(span, out var written);
            if (operationStatus != OperationStatus.Done)
            {
                throw new NodeIdInvalidFormatException(formattedId);
            }

            span = span[..written];

            return ParseDecodedData(span, formattedId, getType);
        }
        finally
        {
            Clear(rentedBuffer);
        }
    }

    private NodeId ParseHex(string formattedId, Func<string, Type?> getType)
    {
        byte[]? rentedDecodeBuffer = null;
        var expectedDecodedSize = formattedId.Length / 2; // Each pair of hex chars = 1 byte

        var decodedIdSpan = expectedDecodedSize <= StackallocThreshold
            ? stackalloc byte[expectedDecodedSize]
            : rentedDecodeBuffer = ArrayPool<byte>.Shared.Rent(expectedDecodedSize);

        try
        {
#if NET9_0_OR_GREATER
            var status = Convert.FromHexString(formattedId, decodedIdSpan, out _, out var written);

            if (status is not OperationStatus.Done)
            {
                throw new NodeIdInvalidFormatException(formattedId);
            }

            decodedIdSpan = decodedIdSpan[..written];
#else
            var buffer = Convert.FromHexString(formattedId);
            buffer.CopyTo(decodedIdSpan);
            decodedIdSpan = decodedIdSpan[..buffer.Length];
#endif

            return ParseDecodedData(decodedIdSpan, formattedId, getType);
        }
        finally
        {
            Clear(rentedDecodeBuffer);
        }
    }

    private NodeId ParseBase36(string formattedId, Func<string, Type?> getType)
    {
        byte[]? rentedDecodeBuffer = null;
        var expectedDecodedSize = Base36.GetByteCount(formattedId);

        var decodedIdSpan = expectedDecodedSize <= StackallocThreshold
            ? stackalloc byte[expectedDecodedSize]
            : rentedDecodeBuffer = ArrayPool<byte>.Shared.Rent(expectedDecodedSize);

        try
        {
            var written = Base36.Decode(formattedId, decodedIdSpan);
            decodedIdSpan = decodedIdSpan[..written];

            return ParseDecodedData(decodedIdSpan, formattedId, getType);
        }
        catch (ArgumentException)
        {
            throw new NodeIdInvalidFormatException(formattedId);
        }
        finally
        {
            Clear(rentedDecodeBuffer);
        }
    }

    private NodeId ParseDecodedData(ReadOnlySpan<byte> span, string originalFormattedId, Func<string, Type?> getType)
    {
        var delimiterIndex = FindDelimiterIndex(span);
        if (delimiterIndex == -1)
        {
            throw new NodeIdInvalidFormatException(originalFormattedId);
        }

        var delimiterOffset = 1;
        if (span[delimiterIndex] == LegacyDelimiter)
        {
            delimiterOffset = 2;
        }

        var typeName = span[..delimiterIndex];
        if (!_spanSerializerMap.TryGetValue(typeName, out var serializer))
        {
            var typeNameString = ToString(typeName);
            var runtimeType = getType(typeNameString) ?? typeof(string);
            var valueSerializer = TryResolveSerializer(runtimeType);

            if (valueSerializer is null)
            {
                throw new NodeIdMissingSerializerException(typeNameString);
            }

            lock (_spanSerializerMap)
            {
                if (!_spanSerializerMap.TryGetValue(typeName, out serializer))
                {
                    serializer = new Serializer(
                        typeNameString,
                        valueSerializer,
                        _outputNewIdFormat,
                        _format);
                    _spanSerializerMap.Add(serializer.FormattedTypeName, serializer);
                }
            }
        }

        return serializer.Parse(span[(delimiterIndex + delimiterOffset)..]);
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

    // ReSharper disable once UseUtf8StringLiteral
    private static readonly byte[] s_delimiters = [Delimiter, LegacyDelimiter];
    private static readonly SearchValues<byte>
        s_delimiterSearchValues = SearchValues.Create(s_delimiters);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindDelimiterIndex(ReadOnlySpan<byte> span)
    {
        return span.IndexOfAny(s_delimiterSearchValues);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe string ToString(ReadOnlySpan<byte> span)
    {
        fixed (byte* buffer = span)
        {
            return s_utf8.GetString(buffer, span.Length);
        }
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
        NodeIdSerializerFormat format)
    {
        private readonly byte[] _formattedTypeName = s_utf8.GetBytes(typeName);

        public byte[] FormattedTypeName => _formattedTypeName;

        public unsafe string Format(object value)
        {
            var minLength = _formattedTypeName.Length + 128;
            byte[]? rentedBuffer = null;
            var span = minLength <= StackallocThreshold
                ? stackalloc byte[StackallocThreshold]
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
                var formattedId = format switch
                {
                    NodeIdSerializerFormat.Base64
                        => FormatBase64(span, written, urlSafeBase64: false, ref rentedBuffer, capacity),
                    NodeIdSerializerFormat.UrlSafeBase64
                        => FormatBase64(span, written, urlSafeBase64: true, ref rentedBuffer, capacity),
                    NodeIdSerializerFormat.UpperHex
                        => FormatHex(span, written, lowerCase: false),
                    NodeIdSerializerFormat.LowerHex
                        => FormatHex(span, written, lowerCase: true),
                    NodeIdSerializerFormat.Base36
                        => FormatBase36(span, written),
                    _ => throw new NotSupportedException("Unsupported format.")
                };

                Clear(rentedBuffer);
                return formattedId;
            }

            Clear(rentedBuffer);

            throw new NodeIdInvalidFormatException(value);
        }

        private string FormatBase64(Span<byte> span, int written, bool urlSafeBase64, ref byte[]? rentedBuffer, int capacity)
        {
            var delimiterLength = outputNewIdFormat ? 1 : 2;
            var dataLength = _formattedTypeName.Length + delimiterLength + written;

            while (Base64.EncodeToUtf8InPlace(span, dataLength, out written) == OperationStatus.DestinationTooSmall)
            {
                capacity *= 2;
                var newBuffer = ArrayPool<byte>.Shared.Rent(capacity);
                span[..dataLength].CopyTo(newBuffer);
                span = newBuffer;
                capacity = newBuffer.Length;

                if (rentedBuffer is not null)
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                }

                rentedBuffer = newBuffer;
            }

            span = span[..written];

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

            return OptimizedNodeIdSerializer.ToString(span);
        }

        private string FormatHex(Span<byte> span, int written, bool lowerCase)
        {
            var delimiterLength = outputNewIdFormat ? 1 : 2;
            var dataLength = _formattedTypeName.Length + delimiterLength + written;
            var sourceData = span[..dataLength];

#if NET9_0_OR_GREATER
            return lowerCase ? Convert.ToHexStringLower(sourceData) : Convert.ToHexString(sourceData);
#else
            var value = Convert.ToHexString(sourceData);
            return lowerCase ? value.ToLowerInvariant() : value;
#endif
        }

        private string FormatBase36(Span<byte> span, int written)
        {
            var delimiterLength = outputNewIdFormat ? 1 : 2;
            var dataLength = _formattedTypeName.Length + delimiterLength + written;
            var sourceData = span[..dataLength];

            return Base36.Encode(sourceData);
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

            var valueSpan = span[typeName.Length..];

            if (outputNewIdFormat)
            {
                valueSpan[0] = Delimiter;
                return valueSpan[1..];
            }

            valueSpan[0] = LegacyDelimiter;
            valueSpan[1] = LegacyNodeIdSerializer.GetLegacyValueCode(value);
            return valueSpan[2..];
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

            bucket[insertAt] = new Entry { HashCode = hashCode, Key = formattedTypeName, Value = serializer };
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

            serializer = null;
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
