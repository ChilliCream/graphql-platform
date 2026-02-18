using System.Runtime.CompilerServices;
#if NET8_0_OR_GREATER
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
#endif
using static HotChocolate.Language.Properties.LangUtf8Resources;

namespace HotChocolate.Language;

internal static class Utf8Helper
{
    public static void Unescape(
        in ReadOnlySpan<byte> escapedString,
        ref Span<byte> unescapedString,
        bool isBlockString)
    {
        if (escapedString.Length == 0)
        {
            if (unescapedString.Length > 0)
            {
                unescapedString = unescapedString.Slice(0, 0);
            }
            return;
        }

        // Fast path: no escapes just copy.
        var firstBackslash = escapedString.IndexOf(GraphQLCharacters.Backslash);
        if (firstBackslash == -1)
        {
            escapedString.CopyTo(unescapedString);
            unescapedString = unescapedString.Slice(0, escapedString.Length);
            return;
        }

        // Copy everything before first backslash
        if (firstBackslash > 0)
        {
            escapedString.Slice(0, firstBackslash).CopyTo(unescapedString);
        }

        var readPos = firstBackslash;
        var writePos = firstBackslash;

        // -1 means no surrogate pending
        var highSurrogate = -1;

        // Process the first escape we already found
        ProcessEscapeSequence(
            escapedString, unescapedString,
            ref readPos, ref writePos,
            ref highSurrogate, isBlockString);

#if NET8_0_OR_GREATER
        var remaining = escapedString.Length - readPos;

        // Vector256 path (32 bytes at a time) if we have enough bytes remain
        if (Vector256.IsHardwareAccelerated && remaining >= Vector256<byte>.Count)
        {
            ref var srcStart = ref MemoryMarshal.GetReference(escapedString);
            ref var dstStart = ref MemoryMarshal.GetReference(unescapedString);
            var backslashVec = Vector256.Create(GraphQLCharacters.Backslash);

            while (readPos <= escapedString.Length - Vector256<byte>.Count)
            {
                var chunk = Vector256.LoadUnsafe(ref srcStart, (nuint)readPos);
                var matches = Vector256.Equals(chunk, backslashVec);
                var mask = matches.ExtractMostSignificantBits();

                if (mask == 0)
                {
                    // No escapes in 32 bytes so we simply copy
                    chunk.StoreUnsafe(ref dstStart, (nuint)writePos);
                    readPos += Vector256<byte>.Count;
                    writePos += Vector256<byte>.Count;
                }
                else
                {
                    // Found backslash, copy up to it, then handle escape
                    var firstEscape = BitOperations.TrailingZeroCount(mask);
                    if (firstEscape > 0)
                    {
                        escapedString.Slice(readPos, firstEscape)
                            .CopyTo(unescapedString.Slice(writePos));
                        writePos += firstEscape;
                    }
                    readPos += firstEscape;

                    ProcessEscapeSequence(
                        escapedString, unescapedString,
                        ref readPos, ref writePos,
                        ref highSurrogate, isBlockString);
                }
            }
        }
        // Vector128 fallback (16 bytes at a time), if we have enough bytes remaining
        else if (Vector128.IsHardwareAccelerated && remaining >= Vector128<byte>.Count)
        {
            ref var srcStart = ref MemoryMarshal.GetReference(escapedString);
            ref var dstStart = ref MemoryMarshal.GetReference(unescapedString);
            var backslashVec = Vector128.Create(GraphQLCharacters.Backslash);

            while (readPos <= escapedString.Length - Vector128<byte>.Count)
            {
                var chunk = Vector128.LoadUnsafe(ref srcStart, (nuint)readPos);
                var matches = Vector128.Equals(chunk, backslashVec);
                var mask = matches.ExtractMostSignificantBits();

                if (mask == 0)
                {
                    // No escapes in 16 bytes so we simply copy
                    chunk.StoreUnsafe(ref dstStart, (nuint)writePos);
                    readPos += Vector128<byte>.Count;
                    writePos += Vector128<byte>.Count;
                }
                else
                {
                    // Found backslash, copy up to it, then handle escape
                    var firstEscape = BitOperations.TrailingZeroCount(mask);
                    if (firstEscape > 0)
                    {
                        escapedString.Slice(readPos, firstEscape)
                            .CopyTo(unescapedString.Slice(writePos));
                        writePos += firstEscape;
                    }
                    readPos += firstEscape;

                    ProcessEscapeSequence(
                        escapedString, unescapedString,
                        ref readPos, ref writePos,
                        ref highSurrogate, isBlockString);
                }
            }
        }
#endif

        // Scalar tail for remaining bytes
        while (readPos < escapedString.Length)
        {
            var code = escapedString[readPos];

            if (code == GraphQLCharacters.Backslash)
            {
                ProcessEscapeSequence(
                    escapedString, unescapedString,
                    ref readPos, ref writePos,
                    ref highSurrogate, isBlockString);
            }
            else
            {
                unescapedString[writePos++] = code;
                readPos++;
            }
        }

        if (unescapedString.Length > writePos)
        {
            unescapedString = unescapedString.Slice(0, writePos);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessEscapeSequence(
        in ReadOnlySpan<byte> escaped,
        Span<byte> unescaped,
        ref int readPos,
        ref int writePos,
        ref int highSurrogate,
        bool isBlockString)
    {
        if (readPos + 1 >= escaped.Length)
        {
            throw new Utf8EncodingException(
                string.Format(Utf8Helper_InvalidEscapeChar, '\\'));
        }

        // skip backslash
        readPos++;
        var code = escaped[readPos++];

        if (isBlockString && code == GraphQLCharacters.Quote)
        {
            if (readPos + 1 < escaped.Length
                && escaped[readPos] == GraphQLCharacters.Quote
                && escaped[readPos + 1] == GraphQLCharacters.Quote)
            {
                readPos += 2;
                unescaped[writePos++] = GraphQLCharacters.Quote;
                unescaped[writePos++] = GraphQLCharacters.Quote;
                unescaped[writePos++] = GraphQLCharacters.Quote;
            }
            else
            {
                throw new Utf8EncodingException(Utf8Helper_InvalidQuoteEscapeCount);
            }
        }
        else if (code.IsValidEscapeCharacter())
        {
            if (code == GraphQLCharacters.U)
            {
                if (readPos + 3 >= escaped.Length)
                {
                    throw new Utf8EncodingException(
                        string.Format(Utf8Helper_InvalidEscapeChar, 'u'));
                }

                var unicodeDecimal = UnescapeUtf8Hex(
                    escaped[readPos],
                    escaped[readPos + 1],
                    escaped[readPos + 2],
                    escaped[readPos + 3]);
                readPos += 4;

                if (unicodeDecimal >= 0xD800 && unicodeDecimal <= 0xDBFF)
                {
                    // High surrogate
                    if (highSurrogate >= 0)
                    {
                        throw new Utf8EncodingException("Unexpected high surrogate.");
                    }
                    highSurrogate = unicodeDecimal;
                }
                else if (unicodeDecimal >= 0xDC00 && unicodeDecimal <= 0xDFFF)
                {
                    // Low surrogate
                    if (highSurrogate < 0)
                    {
                        throw new Utf8EncodingException("Unexpected low surrogate.");
                    }
                    var fullUnicode = ((highSurrogate - 0xD800) << 10)
                        + (unicodeDecimal - 0xDC00)
                        + 0x10000;
                    UnescapeUtf8Hex(fullUnicode, ref writePos, unescaped);
                    highSurrogate = -1;
                }
                else
                {
                    if (highSurrogate >= 0)
                    {
                        throw new Utf8EncodingException("High surrogate not followed by low surrogate.");
                    }
                    UnescapeUtf8Hex(unicodeDecimal, ref writePos, unescaped);
                }
            }
            else
            {
                unescaped[writePos++] = code.EscapeCharacter();
            }
        }
        else
        {
            throw new Utf8EncodingException(
                string.Format(
                    Utf8Helper_InvalidEscapeChar,
                    (char)code));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnescapeUtf8Hex(byte a, byte b, byte c, byte d)
        => (HexToDecimal(a) << 12) | (HexToDecimal(b) << 8) | (HexToDecimal(c) << 4) | HexToDecimal(d);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnescapeUtf8Hex(
        int unicodeDecimal,
        ref int writePosition,
        Span<byte> unescapedString)
    {
        if (unicodeDecimal < 0x80)
        {
            unescapedString[writePosition++] = (byte)unicodeDecimal;
        }
        else if (unicodeDecimal < 0x800)
        {
            unescapedString[writePosition++] = (byte)(0xC0 | (unicodeDecimal >> 6));
            unescapedString[writePosition++] = (byte)(0x80 | (unicodeDecimal & 0x3F));
        }
        else if (unicodeDecimal < 0x10000)
        {
            unescapedString[writePosition++] = (byte)(0xE0 | (unicodeDecimal >> 12));
            unescapedString[writePosition++] = (byte)(0x80 | ((unicodeDecimal >> 6) & 0x3F));
            unescapedString[writePosition++] = (byte)(0x80 | (unicodeDecimal & 0x3F));
        }
        else
        {
            unescapedString[writePosition++] = (byte)(0xF0 | (unicodeDecimal >> 18));
            unescapedString[writePosition++] = (byte)(0x80 | ((unicodeDecimal >> 12) & 0x3F));
            unescapedString[writePosition++] = (byte)(0x80 | ((unicodeDecimal >> 6) & 0x3F));
            unescapedString[writePosition++] = (byte)(0x80 | (unicodeDecimal & 0x3F));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int HexToDecimal(int a)
    {
        return a switch
        {
            >= 48 and <= 57 => a - 48,
            >= 65 and <= 70 => a - 55,
            >= 97 and <= 102 => a - 87,
            _ => -1
        };
    }
}
