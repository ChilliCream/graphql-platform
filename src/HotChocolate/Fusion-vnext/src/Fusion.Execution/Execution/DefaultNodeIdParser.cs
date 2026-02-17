using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Buffers.Text;
using HotChocolate.Execution.Relay;

namespace HotChocolate.Fusion.Execution;

internal sealed class DefaultNodeIdParser : INodeIdParser
{
    private static readonly SearchValues<byte> s_delimiterSearchValues = SearchValues.Create(":\n"u8);
    private readonly Encoding _utf8 = Encoding.UTF8;
    private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
    private readonly NodeIdSerializerFormat _format;

    public DefaultNodeIdParser(NodeIdSerializerFormat format = NodeIdSerializerFormat.UrlSafeBase64)
    {
        _format = format;
    }

    public bool TryParseTypeName(string id, [NotNullWhen(true)] out string? typeName)
    {
        if (string.IsNullOrEmpty(id))
        {
            typeName = null;
            return false;
        }

        return _format switch
        {
            NodeIdSerializerFormat.Base64 or NodeIdSerializerFormat.UrlSafeBase64
                => TryParseTypeNameBase64(id, out typeName),
            NodeIdSerializerFormat.UpperHex or NodeIdSerializerFormat.LowerHex
                => TryParseTypeNameHex(id, out typeName),
            NodeIdSerializerFormat.Base36
                => TryParseTypeNameBase36(id, out typeName),
            _ => throw new NotSupportedException("Unsupported format.")
        };
    }

    private bool TryParseTypeNameBase64(string id, [NotNullWhen(true)] out string? typeName)
    {
        // Base64 might need up to 3 padding characters, so we allocate extra space
        var size = _utf8.GetByteCount(id) + 3;
        byte[]? buffer = null;
        var span = size <= 256 ? stackalloc byte[256] : buffer = _arrayPool.Rent(size);

        try
        {
            // Convert string to UTF-8 bytes
            var actualLength = _utf8.GetBytes(id, span);

            // Handle URL-safe Base64 conversion in place
            if (_format is NodeIdSerializerFormat.UrlSafeBase64)
            {
                ConvertFromUrlSafeBase64InPlace(span[..actualLength]);
            }

            // Ensure correct padding for Base64
            var paddedLength = EnsureBase64Padding(span, actualLength);
            var status = Base64.DecodeFromUtf8InPlace(span[..paddedLength], out var written);

            if (status is OperationStatus.Done)
            {
                return TryExtractTypeName(span[..written], out typeName);
            }

            typeName = null;
            return false;
        }
        finally
        {
            if (buffer is not null)
            {
                _arrayPool.Return(buffer);
            }
        }
    }

    private bool TryParseTypeNameHex(string id, [NotNullWhen(true)] out string? typeName)
    {
        if (id.Length % 2 != 0)
        {
            typeName = null;
            return false;
        }

        var decodedSize = id.Length / 2;
        byte[]? buffer = null;
        var span = decodedSize <= 256 ? stackalloc byte[decodedSize] : buffer = _arrayPool.Rent(decodedSize);

        try
        {
#if NET9_0_OR_GREATER
            if (Convert.FromHexString(id, span, out _, out var written) != OperationStatus.Done)
            {
                typeName = null;
                return false;
            }
#else
            byte[] decoded;
            try
            {
                decoded = Convert.FromHexString(id);
            }
            catch (FormatException)
            {
                typeName = null;
                return false;
            }

            if (decoded.Length > span.Length)
            {
                typeName = null;
                return false;
            }

            decoded.CopyTo(span);
            var written = decoded.Length;
#endif

            return TryExtractTypeName(span[..written], out typeName);
        }
        finally
        {
            if (buffer is not null)
            {
                _arrayPool.Return(buffer);
            }
        }
    }

    private bool TryParseTypeNameBase36(string id, [NotNullWhen(true)] out string? typeName)
    {
        var expectedSize = Base36.GetByteCount(id);
        byte[]? buffer = null;
        var span = expectedSize <= 256 ? stackalloc byte[expectedSize] : buffer = _arrayPool.Rent(expectedSize);

        try
        {
            var written = Base36.Decode(id, span);
            return TryExtractTypeName(span[..written], out typeName);
        }
        catch (ArgumentException)
        {
            typeName = null;
            return false;
        }
        finally
        {
            if (buffer is not null)
            {
                _arrayPool.Return(buffer);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryExtractTypeName(ReadOnlySpan<byte> decodedData, [NotNullWhen(true)] out string? typeName)
    {
        var delimiterIndex = decodedData.IndexOfAny(s_delimiterSearchValues);

        if (delimiterIndex < 0 || delimiterIndex >= decodedData.Length - 1)
        {
            typeName = null;
            return false;
        }

        var typeNameSpan = decodedData[..delimiterIndex];
        typeName = _utf8.GetString(typeNameSpan);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ConvertFromUrlSafeBase64InPlace(Span<byte> span)
    {
        // Convert URL-safe Base64 back to standard Base64 in place
        for (var i = 0; i < span.Length; i++)
        {
            span[i] = span[i] switch
            {
                (byte)'-' => (byte)'+',
                (byte)'_' => (byte)'/',
                var b => b
            };
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int EnsureBase64Padding(Span<byte> buffer, int length)
    {
        var nonPaddedLength = length;

        // Find existing padding
        for (var i = length - 1; i >= 0; i--)
        {
            if (buffer[i] != '=')
            {
                nonPaddedLength = i + 1;
                break;
            }
        }

        var expectedPadding = (4 - (nonPaddedLength % 4)) % 4;
        var paddedLength = nonPaddedLength + expectedPadding;

        // Add correct padding
        for (var i = nonPaddedLength; i < paddedLength; i++)
        {
            buffer[i] = (byte)'=';
        }

        return paddedLength;
    }
}
