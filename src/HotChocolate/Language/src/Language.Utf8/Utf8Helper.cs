using System;
using System.Runtime.CompilerServices;
using static HotChocolate.Language.Properties.LangUtf8Resources;

namespace HotChocolate.Language
{
    internal static class Utf8Helper
    {
        private const int _utf8TwoByteMask = 0b1100_0000_1000_0000;
        private const int _utf8ThreeByteMask = 0b1110_0000_1000_0000_1000_0000;
        private const int _shiftBytesMask = 0b1111_1111_1100_0000;

        public static void Unescape(
            in ReadOnlySpan<byte> escapedString,
            ref Span<byte> unescapedString,
            bool isBlockString)
        {
            var readPosition = -1;
            var writePosition = 0;
            var eofPosition = escapedString.Length - 1;

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
                                UnescapeUtf8Hex(
                                    escapedString[++readPosition],
                                    escapedString[++readPosition],
                                    escapedString[++readPosition],
                                    escapedString[++readPosition],
                                    ref writePosition,
                                    ref unescapedString);
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
                                    (char)code));
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnescapeUtf8Hex(
            byte a, byte b, byte c, byte d,
            ref int writePosition,
            ref Span<byte> unescapedString)
        {
            var unicodeDecimal = (HexToDecimal(a) << 12)
                                 | (HexToDecimal(b) << 8)
                                 | (HexToDecimal(c) << 4)
                                 | HexToDecimal(d);

            if (unicodeDecimal is >= 0 and <= 127)
            {
                unescapedString[writePosition++] = (byte)unicodeDecimal;
            }
            else if (unicodeDecimal is >= 128 and <= 2047)
            {
                var bytesToShift = unicodeDecimal & _shiftBytesMask;
                unicodeDecimal -= bytesToShift;
                bytesToShift <<= 2;
                unicodeDecimal += _utf8TwoByteMask + bytesToShift;

                unescapedString[writePosition++] = (byte)(unicodeDecimal >> 8);
                unescapedString[writePosition++] = (byte)unicodeDecimal;
            }
            else if (unicodeDecimal is >= 2048 and <= 65535)
            {
                var bytesToShift = unicodeDecimal & _shiftBytesMask;
                unicodeDecimal -= bytesToShift;

                var third = (bytesToShift >> 12) << 12;
                var second = bytesToShift - third;

                second <<= 2;
                third <<= 4;

                unicodeDecimal += _utf8ThreeByteMask + second + third;

                unescapedString[writePosition++] = (byte)(unicodeDecimal >> 16);
                unescapedString[writePosition++] = (byte)(unicodeDecimal >> 8);
                unescapedString[writePosition++] = (byte)unicodeDecimal;
            }
            else
            {
                throw new NotSupportedException(
                    "UTF-8 characters with four bytes are not supported.");
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

}
