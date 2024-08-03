using System.Buffers;

namespace HotChocolate.Pagination.Serialization;

public static class CursorKeySerializerHelper
{
    public static ReadOnlySpan<byte> Null => "\\null"u8;
    public static ReadOnlySpan<byte> EscapedNull => "\\\\null"u8;

    public static ReadOnlySpan<byte> EscapedColon => "\\:"u8;

    public static object? Parse(ReadOnlySpan<byte> formattedKey, ICursorKeySerializer serializer)
    {
        if (formattedKey.SequenceEqual(Null))
        {
            return null;
        }

        if (formattedKey.SequenceEqual(EscapedNull))
        {
            return "\\null";
        }

        var start = formattedKey.IndexOf(EscapedColon);

        if (start == -1)
        {
            return serializer.Parse(formattedKey);
        }

        return RestoreColons(formattedKey, start, serializer);
    }

    private static object? RestoreColons(
        ReadOnlySpan<byte> formattedKey,
        int start,
        ICursorKeySerializer serializer)
    {
        byte[]? rented = null;
        var buffer = formattedKey.Length <= 256
            ? stackalloc byte[formattedKey.Length]
            : rented = ArrayPool<byte>.Shared.Rent(formattedKey.Length);

        formattedKey.Slice(0, start).CopyTo(buffer);

        var i = start;
        var j = start;

        while (j < formattedKey.Length)
        {
            var c = formattedKey[j++];

            if (c == (byte)'\\' && formattedKey[j] == (byte)':')
            {
                buffer[i++] = (byte)':';
                j++;
            }
            else
            {
                buffer[i++] = c;
            }
        }

        var key = serializer.Parse(buffer.Slice(0, i));

        if (rented != null)
        {
            ArrayPool<byte>.Shared.Return(rented);
        }

        return key;
    }

    public static bool TryFormat(object? key, ICursorKeySerializer serializer, Span<byte> buffer, out int written)
    {
        var success = false;

        if (key is null)
        {
            success = Null.TryCopyTo(buffer);
            written = success ? Null.Length : 0;
            return success;
        }

        if (key is string s && s.Equals("\\null", StringComparison.Ordinal))
        {
            success = EscapedNull.TryCopyTo(buffer);
            written = success ? EscapedNull.Length : 0;
            return success;
        }

        success = serializer.TryFormat(key, buffer, out written);

        if (!success)
        {
            return success;
        }

        var start = buffer.IndexOf((byte)':');
        return start == -1 ? success : ReplaceColons(buffer, start, written, out written);
    }

    private static bool ReplaceColons(Span<byte> original, int start, int length, out int written)
    {
        written = 0;

        if (length == original.Length)
        {
            return false;
        }

        byte[]? rented = null;
        var buffer = original.Length <= 256
            ? stackalloc byte[original.Length]
            : rented = ArrayPool<byte>.Shared.Rent(original.Length);

        original.Slice(0, start).CopyTo(buffer);

        var i = start;
        var j = start;

        while (j < length)
        {
            var c = original[j++];

            if (c == (byte)':')
            {
                if (i + 1 >= original.Length)
                {
                    if (rented != null)
                    {
                        ArrayPool<byte>.Shared.Return(rented);
                    }
                    written = 0;
                    return false;
                }

                buffer[i++] = (byte)'\\';
                buffer[i++] = (byte)':';
            }
            else
            {
                buffer[i++] = c;
            }
        }

        buffer.Slice(0, i).CopyTo(original);
        written = i;

        if (rented != null)
        {
            ArrayPool<byte>.Shared.Return(rented);
        }

        return true;
    }
}
