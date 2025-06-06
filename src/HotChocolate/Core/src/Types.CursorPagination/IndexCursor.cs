using System.Buffers;
using System.Buffers.Text;
using System.Text;

namespace HotChocolate.Types.Pagination;

internal static class IndexCursor
{
    private static readonly Encoding s_utf8 = Encoding.UTF8;

    public static unsafe string Format(Span<byte> buffer)
    {
        fixed (byte* bytePtr = buffer)
        {
            return s_utf8.GetString(bytePtr, buffer.Length);
        }
    }

    public static unsafe bool TryParse(string cursor, out int index)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            index = -1;
            return false;
        }

        fixed (char* cPtr = cursor)
        {
            var count = s_utf8.GetByteCount(cPtr, cursor.Length);
            byte[]? rented = null;

            var buffer = count <= 128
                ? stackalloc byte[count]
                : rented = ArrayPool<byte>.Shared.Rent(count);

            try
            {
                fixed (byte* bytePtr = buffer)
                {
                    s_utf8.GetBytes(cPtr, cursor.Length, bytePtr, buffer.Length);
                }

                Base64.DecodeFromUtf8InPlace(buffer, out var written);
                if (Utf8Parser.TryParse(buffer[..written], out index, out _))
                {
                    return true;
                }

                index = -1;
                return false;
            }
            finally
            {
                if (rented is not null)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }
    }
}
