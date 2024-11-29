using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Types.Relay;
using MongoDB.Bson;

namespace HotChocolate.Data.MongoDb.Relay;

internal sealed class ObjectIdNodeIdValueSerializer(bool compress = true) : INodeIdValueSerializer
{
    private byte[]? _buffer = new byte[12];

    public bool IsSupported(Type type) => type == typeof(ObjectId) || type == typeof(ObjectId?);

    public NodeIdFormatterResult Format(Span<byte> buffer, object value, out int written)
    {
        if (value is ObjectId o)
        {
            if (compress)
            {
                if(buffer.Length < 12)
                {
                    written = 0;
                    return NodeIdFormatterResult.BufferTooSmall;
                }

                var rawBytes = ArrayPool<byte>.Shared.Rent(12);
                o.ToByteArray(rawBytes, 0);
                rawBytes.AsSpan().Slice(0, 12).CopyTo(buffer);
                ArrayPool<byte>.Shared.Return(rawBytes);
                written = 12;
                return NodeIdFormatterResult.Success;
            }

            if(buffer.Length < 24)
            {
                written = 0;
                return NodeIdFormatterResult.BufferTooSmall;
            }

            TryWrite(buffer, o);
            written = 24;
            return NodeIdFormatterResult.Success;
        }

        written = 0;
        return NodeIdFormatterResult.InvalidValue;
    }

    private static void TryWrite(Span<byte> buffer, ObjectId o)
    {
        var rawBytes = ArrayPool<byte>.Shared.Rent(12);
        o.ToByteArray(rawBytes, 0);
        FromByteArray(rawBytes, 0, out var a, out var b, out var c);

        buffer[0] = ToHexChar((a >> 28) & 0x0f);
        buffer[1] = ToHexChar((a >> 24) & 0x0f);
        buffer[2] = ToHexChar((a >> 20) & 0x0f);
        buffer[3] = ToHexChar((a >> 16) & 0x0f);
        buffer[4] = ToHexChar((a >> 12) & 0x0f);
        buffer[5] = ToHexChar((a >> 8) & 0x0f);
        buffer[6] = ToHexChar((a >> 4) & 0x0f);
        buffer[7] = ToHexChar(a & 0x0f);
        buffer[8] = ToHexChar((b >> 28) & 0x0f);
        buffer[9] = ToHexChar((b >> 24) & 0x0f);
        buffer[10] =ToHexChar((b >> 20) & 0x0f);
        buffer[11] =ToHexChar((b >> 16) & 0x0f);
        buffer[12] =ToHexChar((b >> 12) & 0x0f);
        buffer[13] = ToHexChar((b >> 8) & 0x0f);
        buffer[14] = ToHexChar((b >> 4) & 0x0f);
        buffer[15] = ToHexChar(b & 0x0f);
        buffer[16] = ToHexChar((c >> 28) & 0x0f);
        buffer[17] = ToHexChar((c >> 24) & 0x0f);
        buffer[18] = ToHexChar((c >> 20) & 0x0f);
        buffer[19] = ToHexChar((c >> 16) & 0x0f);
        buffer[20] = ToHexChar((c >> 12) & 0x0f);
        buffer[21] = ToHexChar((c >> 8) & 0x0f);
        buffer[22] = ToHexChar((c >> 4) & 0x0f);
        buffer[23] = ToHexChar(c & 0x0f);

        ArrayPool<byte>.Shared.Return(rawBytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ToHexChar(int value)
        => (byte)(value + (value < 10 ? (byte)'0' : (byte)'a' - 10));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FromByteArray(byte[] bytes, int offset, out int a, out int b, out int c)
    {
        a = (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
        b = (bytes[offset + 4] << 24) | (bytes[offset + 5] << 16) | (bytes[offset + 6] << 8) | bytes[offset + 7];
        c = (bytes[offset + 8] << 24) | (bytes[offset + 9] << 16) | (bytes[offset + 10] << 8) | bytes[offset + 11];
    }

    public bool TryParse(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out object? value)
    {
        byte[]? rawBytes;

        if (compress)
        {
            if (buffer.Length != 12)
            {
                value = null;
                return false;
            }

            rawBytes = Interlocked.Exchange(ref _buffer, null) ?? new byte[12];
            buffer.CopyTo(rawBytes);
            value = new ObjectId(rawBytes);
            Interlocked.Exchange(ref _buffer, rawBytes);
            return true;
        }

        if(buffer.Length != 24)
        {
            value = null;
            return false;
        }

        rawBytes = Interlocked.Exchange(ref _buffer, null) ?? new byte[12];
        if (TryParseHexString(buffer, rawBytes))
        {
            value = new ObjectId(rawBytes);
            Interlocked.Exchange(ref _buffer, rawBytes);
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryParseHexString(ReadOnlySpan<byte> formattedId, byte[] rawBytes)
    {
        if (formattedId.IsEmpty)
        {
            return false;
        }

        var i = 0;
        var j = 0;

        if (formattedId.Length % 2 == 1)
        {
            // if s has an odd length assume an implied leading "0"
            if (!TryParseHexChar(formattedId[i++], out var y))
            {
                return false;
            }
            rawBytes[j++] = (byte)y;
        }

        while (i < formattedId.Length)
        {
            if (!TryParseHexChar(formattedId[i++], out var x))
            {
                return false;
            }
            if (!TryParseHexChar(formattedId[i++], out var y))
            {
                return false;
            }
            rawBytes[j++] = (byte)((x << 4) | y);
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseHexChar(byte c, out int value)
    {
        if (c >= '0' && c <= '9')
        {
            value = c - '0';
            return true;
        }

        if (c >= 'a' && c <= 'f')
        {
            value = 10 + (c - 'a');
            return true;
        }

        if (c >= 'A' && c <= 'F')
        {
            value = 10 + (c - 'A');
            return true;
        }

        value = 0;
        return false;
    }
}
