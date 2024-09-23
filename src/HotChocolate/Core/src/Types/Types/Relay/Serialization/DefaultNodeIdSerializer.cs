#nullable enable
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using HotChocolate.Language;

namespace HotChocolate.Types.Relay;

public sealed class DefaultNodeIdSerializer(
    INodeIdValueSerializer[]? serializers = null,
    int maxIdLength = 1024,
    bool outputNewIdFormat = true,
    bool urlSafeBase64 = true)
    : INodeIdSerializer
{
    private const byte _delimiter = (byte)':';
    private const byte _legacyDelimiter = (byte)'\n';
    private const int _stackallocThreshold = 256;
    private static readonly Encoding _utf8 = Encoding.UTF8;
    private readonly ConcurrentDictionary<string, byte[]> _names = new();
    private readonly INodeIdValueSerializer[] _serializers = serializers ??
    [
        new StringNodeIdValueSerializer(),
        new Int16NodeIdValueSerializer(),
        new Int32NodeIdValueSerializer(),
        new Int64NodeIdValueSerializer(),
        new GuidNodeIdValueSerializer()
    ];

    public string Format(string typeName, object internalId)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            throw new ArgumentException(
                "Value cannot be null or empty.",
                nameof(typeName));
        }

        if (internalId == null)
        {
            throw new ArgumentNullException(nameof(internalId));
        }

        var runtimeType = internalId.GetType();
        var serializer = TryResolveSerializer(runtimeType);

        if (serializer is null)
        {
            throw SerializerMissing(typeName);
        }

        return Format(
            _names.GetOrAdd(typeName, static n => _utf8.GetBytes(n)),
            internalId,
            serializer,
            outputNewIdFormat,
            urlSafeBase64);
    }

    private static unsafe string Format(
        ReadOnlySpan<byte> typeName,
        object internalId,
        INodeIdValueSerializer serializer,
        bool outputNewIdFormat,
        bool urlSafeBase64)
    {
        var minLength = typeName.Length + 128;
        byte[]? rentedBuffer = null;
        var span = minLength <= _stackallocThreshold
            ? stackalloc byte[_stackallocThreshold]
            : rentedBuffer = ArrayPool<byte>.Shared.Rent(minLength);
        var capacity = span.Length;

        var valueSpan = WriteIdHeader(span, typeName, internalId, outputNewIdFormat);

        var result = serializer.Format(valueSpan, internalId, out var written);

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

            valueSpan = WriteIdHeader(span, typeName, internalId, outputNewIdFormat);

            result = serializer.Format(valueSpan, internalId, out written);
        }

        if (result == NodeIdFormatterResult.Success)
        {
            var delimiterLength = outputNewIdFormat ? 1 : 2;
            var dataLength = typeName.Length + delimiterLength + written;

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

            // make urls safe base64
            if(urlSafeBase64)
            {
                for(var i = 0; i < span.Length; i++)
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

            var formattedId = ToString(span);

            Clear(rentedBuffer);
            return formattedId;
        }

        Clear(rentedBuffer);

        throw new NodeIdInvalidFormatException(internalId);
    }

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

    public NodeId Parse(string formattedId, INodeIdRuntimeTypeLookup runtimeTypeLookup)
    {
        if (formattedId is null)
        {
            throw new ArgumentNullException(nameof(formattedId));
        }

        if (formattedId.Length > maxIdLength)
        {
            throw new NodeIdInvalidFormatException(formattedId);
        }

        var expectedSize = _utf8.GetByteCount(formattedId);

        byte[]? rentedBuffer = null;
        var span = expectedSize <= _stackallocThreshold
            ? stackalloc byte[_stackallocThreshold]
            : rentedBuffer = ArrayPool<byte>.Shared.Rent(expectedSize);

        Utf8GraphQLParser.ConvertToBytes(formattedId, ref span);

        if (urlSafeBase64)
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
        var typeNameString = ToString(typeName);
        var runtimeType = runtimeTypeLookup.GetNodeIdRuntimeType(typeNameString) ?? typeof(string);
        var serializer = TryResolveSerializer(runtimeType);

        if (serializer is null)
        {
            Clear(rentedBuffer);
            throw new NodeIdInvalidFormatException(formattedId);
        }

        if (serializer.TryParse(span.Slice(delimiterIndex + delimiterOffset), out var value))
        {
            return new NodeId(typeNameString, value);
        }

        throw SerializerMissing(typeNameString);
    }

    public NodeId Parse(string formattedId, Type runtimeType)
    {
        if (formattedId is null)
        {
            throw new ArgumentNullException(nameof(formattedId));
        }

        if (formattedId.Length > maxIdLength)
        {
            throw new NodeIdInvalidFormatException(formattedId);
        }

        var expectedSize = _utf8.GetByteCount(formattedId);

        byte[]? rentedBuffer = null;
        var span = expectedSize <= _stackallocThreshold
            ? stackalloc byte[_stackallocThreshold]
            : rentedBuffer = ArrayPool<byte>.Shared.Rent(expectedSize);

        Utf8GraphQLParser.ConvertToBytes(formattedId, ref span);

        if (urlSafeBase64)
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
        var typeNameString = ToString(typeName);
        var serializer = TryResolveSerializer(runtimeType);

        if (serializer is null)
        {
            Clear(rentedBuffer);
            throw new NodeIdInvalidFormatException(formattedId);
        }

        if (serializer.TryParse(span.Slice(delimiterIndex + delimiterOffset), out var value))
        {
            return new NodeId(typeNameString, value);
        }

        throw SerializerMissing(typeNameString);
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

    private static readonly byte[] _delimiters = [_delimiter, _legacyDelimiter];
    private static readonly SearchValues<byte> _delimiterSearchValues =
        SearchValues.Create(_delimiters);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindDelimiterIndex(ReadOnlySpan<byte> span)
    {
        return span.IndexOfAny(_delimiterSearchValues);
    }

    private static void Clear(byte[]? rentedBuffer = null)
    {
        if (rentedBuffer is not null)
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

    private static NodeIdMissingSerializerException SerializerMissing(
        string typeName,
        byte[]? rentedBuffer = null)
    {
        Clear(rentedBuffer);
        return new NodeIdMissingSerializerException(typeName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe string ToString(ReadOnlySpan<byte> span)
    {
        fixed (byte* buffer = span)
        {
            return _utf8.GetString(buffer, span.Length);
        }
    }
}
