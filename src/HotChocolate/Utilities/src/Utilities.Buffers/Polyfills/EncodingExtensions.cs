#if NETSTANDARD2_0
using System.Text;

namespace System;

public static class EncodingExtensions
{
    public static unsafe int GetBytes(
        this Encoding encoding,
        string text,
        Span<byte> buffer)
    {
        fixed (byte* bytePtr = buffer)
        {
            fixed (char* stringPtr = text)
            {
                var length = encoding.GetBytes(stringPtr, text.Length, bytePtr, buffer.Length);
                return length;
            }
        }
    }

    public static unsafe string GetString(
        this Encoding encoding,
        ReadOnlySpan<byte> bytes)
    {
        fixed (byte* bytePtr = bytes)
        {
            return encoding.GetString(bytePtr, bytes.Length);
        }
    }
}
#endif
