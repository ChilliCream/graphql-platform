using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Buffers.Text;
using HotChocolate.Caching.Memory;
using HotChocolate.Execution.Relay;

namespace HotChocolate.Types.Relay;

public sealed class DefaultNodeIdSerializer : INodeIdSerializer
{
    private const byte Delimiter = (byte)':';
    private const byte LegacyDelimiter = (byte)'\n';
    private const int StackallocThreshold = 256;
    private static readonly Encoding s_utf8 = Encoding.UTF8;
    private readonly Cache<byte[]> _names;

    private readonly INodeIdValueSerializer[] _serializers;

    private readonly int _maxIdLength;
    private readonly bool _outputNewIdFormat;
    private readonly NodeIdSerializerFormat _format;

    public DefaultNodeIdSerializer(
        INodeIdValueSerializer[]? serializers = null,
        int maxIdLength = 1024,
        bool outputNewIdFormat = true,
        NodeIdSerializerFormat format = NodeIdSerializerFormat.Base64,
        int maxCachedTypeNames = 1024)
    {
        _maxIdLength = maxIdLength;
        _outputNewIdFormat = outputNewIdFormat;
        _format = format;
        _serializers = serializers ??
        [
            new StringNodeIdValueSerializer(),
            new Int16NodeIdValueSerializer(),
            new Int32NodeIdValueSerializer(),
            new Int64NodeIdValueSerializer(),
            new GuidNodeIdValueSerializer()
        ];

        maxCachedTypeNames = maxCachedTypeNames <= 128 ? 128 : maxCachedTypeNames;
        _names = new Cache<byte[]>(maxCachedTypeNames);
    }

    public string Format(string typeName, object internalId)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);
        ArgumentNullException.ThrowIfNull(internalId);

        var runtimeType = internalId.GetType();
        var serializer = TryResolveSerializer(runtimeType);

        if (serializer is null)
        {
            throw SerializerMissing(typeName);
        }

        return Format(
            _names.GetOrCreate(typeName, static n => s_utf8.GetBytes(n)),
            internalId,
            serializer,
            _outputNewIdFormat,
            _format);
    }

    private static unsafe string Format(
        ReadOnlySpan<byte> typeName,
        object internalId,
        INodeIdValueSerializer serializer,
        bool outputNewIdFormat,
        NodeIdSerializerFormat format)
    {
        var minLength = typeName.Length + 128;
        byte[]? rentedBuffer = null;
        var span = minLength <= StackallocThreshold
            ? stackalloc byte[StackallocThreshold]
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
            string formattedId;

            switch (format)
            {
                case NodeIdSerializerFormat.Base64:
                    formattedId = FormatBase64(
                        typeName,
                        span,
                        ref rentedBuffer,
                        capacity,
                        written,
                        outputNewIdFormat,
                        urlSafeBase64: false);
                    break;

                case NodeIdSerializerFormat.UrlSafeBase64:
                    formattedId = FormatBase64(
                        typeName,
                        span,
                        ref rentedBuffer,
                        capacity,
                        written,
                        outputNewIdFormat,
                        urlSafeBase64: true);
                    break;

                case NodeIdSerializerFormat.UpperHex:
                    formattedId = FormatHex(
                        typeName,
                        span,
                        written,
                        outputNewIdFormat,
                        lowerCase: false);
                    break;

                case NodeIdSerializerFormat.LowerHex:
                    formattedId = FormatHex(
                        typeName,
                        span,
                        written,
                        outputNewIdFormat,
                        lowerCase: true);
                    break;

                case NodeIdSerializerFormat.Base36:
                    formattedId = FormatBase36(
                        typeName,
                        span,
                        written,
                        outputNewIdFormat);
                    break;

                default:
                    Clear(rentedBuffer);
                    throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported format.");
            }

            Clear(rentedBuffer);
            return formattedId;
        }

        Clear(rentedBuffer);

        throw new NodeIdInvalidFormatException(internalId);

        static string FormatBase64(
            ReadOnlySpan<byte> typeName,
            Span<byte> span,
            ref byte[]? rentedBuffer,
            int capacity,
            int written,
            bool outputNewIdFormat,
            bool urlSafeBase64)
        {
            var delimiterLength = outputNewIdFormat ? 1 : 2;
            var dataLength = typeName.Length + delimiterLength + written;

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

            // make urls safe base64
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

                span = span.TrimEnd((byte)'=');
            }

            return ToString(span);
        }

        static string FormatHex(
            ReadOnlySpan<byte> typeName,
            Span<byte> span,
            int written,
            bool outputNewIdFormat,
            bool lowerCase)
        {
            var delimiterLength = outputNewIdFormat ? 1 : 2;
            var dataLength = typeName.Length + delimiterLength + written;
            var sourceData = span[..dataLength];

#if NET9_0_OR_GREATER
            return lowerCase ? Convert.ToHexStringLower(sourceData) : Convert.ToHexString(sourceData);
#else
            var value = Convert.ToHexString(sourceData);
            return lowerCase ? value.ToLowerInvariant() : value;
#endif
        }

        static string FormatBase36(
            ReadOnlySpan<byte> typeName,
            Span<byte> span,
            int written,
            bool outputNewIdFormat)
        {
            var delimiterLength = outputNewIdFormat ? 1 : 2;
            var dataLength = typeName.Length + delimiterLength + written;
            var sourceData = span[..dataLength];

            return Base36.Encode(sourceData);
        }
    }

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

        // if we need to produce the legacy identifier format we need
        // to look up the value code for the id value.
        valueSpan[0] = LegacyDelimiter;
        valueSpan[1] = LegacyNodeIdSerializer.GetLegacyValueCode(value);
        return valueSpan[2..];
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
            _ => throw new ArgumentOutOfRangeException(nameof(_format), _format, "Unsupported format.")
        };
    }

    public NodeId Parse(string formattedId, Type runtimeType)
    {
        ArgumentNullException.ThrowIfNull(formattedId);

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

            // Handle URL-safe Base64 conversion
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
            var expectedPadding = (4 - (nonPaddedLength % 4)) % 4;

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
        var expectedDecodedSize = formattedId.Length / 2;

        var decodedIdSpan = expectedDecodedSize <= StackallocThreshold
            ? stackalloc byte[StackallocThreshold]
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
            try
            {
                var buffer = Convert.FromHexString(formattedId);
                buffer.CopyTo(decodedIdSpan);
                decodedIdSpan = decodedIdSpan[..buffer.Length];
            }
            catch (FormatException)
            {
                throw new NodeIdInvalidFormatException(formattedId);
            }
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
        var typeNameString = ToString(typeName);
        var runtimeType = getType(typeNameString) ?? typeof(string);
        var serializer = TryResolveSerializer(runtimeType);

        if (serializer is null)
        {
            throw new NodeIdInvalidFormatException(originalFormattedId);
        }

        if (serializer.TryParse(span[(delimiterIndex + delimiterOffset)..], out var value))
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

    // ReSharper disable once UseUtf8StringLiteral
    private static readonly byte[] s_delimiters = [Delimiter, LegacyDelimiter];
    private static readonly SearchValues<byte> s_delimiterSearchValues =
        SearchValues.Create(s_delimiters);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindDelimiterIndex(ReadOnlySpan<byte> span)
    {
        return span.IndexOfAny(s_delimiterSearchValues);
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
            return s_utf8.GetString(buffer, span.Length);
        }
    }
}
