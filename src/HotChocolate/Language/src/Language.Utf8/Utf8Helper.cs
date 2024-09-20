using System.Runtime.CompilerServices;
using static HotChocolate.Language.Properties.LangUtf8Resources;

namespace HotChocolate.Language;

internal static class Utf8Helper
{
    public static void Unescape(
        in ReadOnlySpan<byte> escapedString,
        ref Span<byte> unescapedString,
        bool isBlockString)
    {
        var readPosition = -1;
        var writePosition = 0;
        var eofPosition = escapedString.Length - 1;
        int? highSurrogate = null;

        if (escapedString.Length > 0)
        {
            do
            {
                var code = escapedString[++readPosition];

                if (code == GraphQLConstants.Backslash)
                {
                    code = escapedString[++readPosition];

                    if (isBlockString && code == GraphQLConstants.Quote)
                    {
                        if (escapedString[readPosition + 1] == GraphQLConstants.Quote
                            && escapedString[readPosition + 2] == GraphQLConstants.Quote)
                        {
                            readPosition += 2;
                            unescapedString[writePosition++] = GraphQLConstants.Quote;
                            unescapedString[writePosition++] = GraphQLConstants.Quote;
                            unescapedString[writePosition++] = GraphQLConstants.Quote;
                        }
                        else
                        {
                            throw new Utf8EncodingException(Utf8Helper_InvalidQuoteEscapeCount);
                        }
                    }
                    else if (code.IsValidEscapeCharacter())
                    {
                        if (code == GraphQLConstants.U)
                        {
                            var unicodeDecimal = UnescapeUtf8Hex(
                                escapedString[++readPosition],
                                escapedString[++readPosition],
                                escapedString[++readPosition],
                                escapedString[++readPosition]);

                            if (unicodeDecimal >= 0xD800 && unicodeDecimal <= 0xDBFF)
                            {
                                // High surrogate
                                if (highSurrogate != null)
                                {
                                    throw new Utf8EncodingException("Unexpected high surrogate.");
                                }
                                highSurrogate = unicodeDecimal;
                            }
                            else if (unicodeDecimal >= 0xDC00 && unicodeDecimal <= 0xDFFF)
                            {
                                // Low surrogate
                                if (highSurrogate == null)
                                {
                                    throw new Utf8EncodingException("Unexpected low surrogate.");
                                }
                                var fullUnicode = ((highSurrogate.Value - 0xD800) << 10) +
                                    (unicodeDecimal - 0xDC00) +
                                    0x10000;
                                UnescapeUtf8Hex(fullUnicode, ref writePosition, unescapedString);
                                highSurrogate = null;
                            }
                            else
                            {
                                if (highSurrogate != null)
                                {
                                    throw new Utf8EncodingException("High surrogate not followed by low surrogate.");
                                }
                                UnescapeUtf8Hex(unicodeDecimal, ref writePosition, unescapedString);
                            }
                        }
                        else
                        {
                            unescapedString[writePosition++] = code.EscapeCharacter();
                        }
                    }
                    else
                    {
                        throw new Utf8EncodingException(
                            string.Format(
                                Utf8Helper_InvalidEscapeChar,
                                (char) code));
                    }
                }
                else
                {
                    unescapedString[writePosition++] = code;
                }
            } while (readPosition < eofPosition);
        }

        if (unescapedString.Length - writePosition > 0)
        {
            unescapedString = unescapedString.Slice(0, writePosition);
        }
    }

    public static int UnescapeUtf8Hex(byte a, byte b, byte c, byte d)
        => (HexToDecimal(a) << 12) | (HexToDecimal(b) << 8) | (HexToDecimal(c) << 4) | HexToDecimal(d);

    public static void UnescapeUtf8Hex(
        int unicodeDecimal,
        ref int writePosition,
        Span<byte> unescapedString)
    {
        if (unicodeDecimal < 0x80)
        {
            unescapedString[writePosition++] = (byte) unicodeDecimal;
        }
        else if (unicodeDecimal < 0x800)
        {
            unescapedString[writePosition++] = (byte) (0xC0 | (unicodeDecimal >> 6));
            unescapedString[writePosition++] = (byte) (0x80 | (unicodeDecimal & 0x3F));
        }
        else if (unicodeDecimal < 0x10000)
        {
            unescapedString[writePosition++] = (byte) (0xE0 | (unicodeDecimal >> 12));
            unescapedString[writePosition++] = (byte) (0x80 | ((unicodeDecimal >> 6) & 0x3F));
            unescapedString[writePosition++] = (byte) (0x80 | (unicodeDecimal & 0x3F));
        }
        else
        {
            unescapedString[writePosition++] = (byte) (0xF0 | (unicodeDecimal >> 18));
            unescapedString[writePosition++] = (byte) (0x80 | ((unicodeDecimal >> 12) & 0x3F));
            unescapedString[writePosition++] = (byte) (0x80 | ((unicodeDecimal >> 6) & 0x3F));
            unescapedString[writePosition++] = (byte) (0x80 | (unicodeDecimal & 0x3F));
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
            _ => -1,
        };
    }
}
