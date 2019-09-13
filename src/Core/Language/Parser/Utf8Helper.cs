using System;
using System.Runtime.CompilerServices;
using HotChocolate.Language.Properties;

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
            int readPosition = -1;
            int writePosition = 0;
            int eofPosition = escapedString.Length - 1;

            do
            {
                byte code = escapedString[++readPosition];

                if (code == GraphQLConstants.Backslash)
                {
                    code = escapedString[++readPosition];

                    if (isBlockString
                         && code == GraphQLConstants.Quote)
                    {
                        if (escapedString[readPosition + 1] ==
                            GraphQLConstants.Quote
                            && escapedString[readPosition + 2] ==
                            GraphQLConstants.Quote)
                        {
                            readPosition += 2;
                            unescapedString[writePosition++] =
                                GraphQLConstants.Quote;
                            unescapedString[writePosition++] =
                                GraphQLConstants.Quote;
                            unescapedString[writePosition++] =
                                GraphQLConstants.Quote;
                        }
                        else
                        {
                            throw new Utf8EncodingException(
                                LangResources.Utf8Helper_InvalidQuoteEscapeCount);
                        }
                    }
                    else if (GraphQLConstants.IsValidEscapeCharacter(code))
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
                            unescapedString[writePosition++] =
                                GraphQLConstants.EscapeCharacter(code);
                        }
                    }
                    else
                    {
                        throw new Utf8EncodingException(
                            LangResources.Utf8Helper_InvalidEscapeChar);
                    }
                }
                else
                {
                    unescapedString[writePosition++] = code;
                }
            } while (readPosition < eofPosition);

            int length = unescapedString.Length - writePosition;
            if (length > 0)
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
            int unicodeDecimal = (HexToDecimal(a) << 12)
                | (HexToDecimal(b) << 8)
                | (HexToDecimal(c) << 4)
                | HexToDecimal(d);

            if (unicodeDecimal >= 0 && unicodeDecimal <= 127)
            {
                unescapedString[writePosition++] = (byte)unicodeDecimal;
            }
            else if (unicodeDecimal >= 128 && unicodeDecimal <= 2047)
            {
                int bytesToShift = unicodeDecimal & _shiftBytesMask;
                unicodeDecimal -= bytesToShift;
                bytesToShift <<= 2;
                unicodeDecimal += _utf8TwoByteMask + bytesToShift;

                unescapedString[writePosition++] = (byte)(unicodeDecimal >> 8);
                unescapedString[writePosition++] = (byte)unicodeDecimal;
            }
            else if (unicodeDecimal >= 2048 && unicodeDecimal <= 65535)
            {
                int bytesToShift = unicodeDecimal & _shiftBytesMask;
                unicodeDecimal -= bytesToShift;

                int third = (bytesToShift >> 12) << 12;
                int second = bytesToShift - third;

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
            return a >= 48 && a <= 57
              ? a - 48 // 0-9
              : a >= 65 && a <= 70
                ? a - 55 // A-F
                : a >= 97 && a <= 102
                  ? a - 87 // a-f
                  : -1;
        }
    }

}
