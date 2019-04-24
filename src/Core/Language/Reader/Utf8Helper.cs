using System;
using System.Runtime.CompilerServices;

namespace HotChocolate.Language
{
    internal static class Utf8Helper
    {
        private const int _utf8TwoByteMask = 0b1100_0000_1000_0000;
        private const int _shiftBytesMask = 0b1111_1111_1100_0000;

        // Escape Triple-Quote (\""")
        public static void Unescape(
            in ReadOnlySpan<byte> escapedString,
            ref Span<byte> unescapedString,
            bool isBlockString)
        {
            int readPosition = 0;
            int writePosition = 0;
            ref readonly byte code = ref escapedString[readPosition];

            while (readPosition < escapedString.Length)
            {
                if (ReaderHelper.IsBackslash(in code))
                {
                    code = ref escapedString[++readPosition];
                    if (ReaderHelper.IsValidEscapeCharacter(code))
                    {
                        unescapedString[writePosition++] =
                            ReaderHelper.EscapeCharacter(in code);
                    }
                    else if (code == ReaderHelper.U)
                    {
                        UnescapeUtf8Hex(
                            in escapedString[++readPosition],
                            in escapedString[++readPosition],
                            in escapedString[++readPosition],
                            in escapedString[++readPosition],
                            ref writePosition,
                            ref unescapedString);
                    }
                    else if (isBlockString
                        && ReaderHelper.IsQuote(
                            in escapedString[readPosition])
                        && ReaderHelper.IsQuote(
                            in escapedString[readPosition + 1])
                        && ReaderHelper.IsQuote(
                            in escapedString[readPosition + 2]))
                    {
                        unescapedString[writePosition++] = ReaderHelper.Quote;
                        unescapedString[writePosition++] = ReaderHelper.Quote;
                        unescapedString[writePosition++] = ReaderHelper.Quote;
                    }
                    else
                    {
                        // TODO : Syntax Exception
                        throw new Exception();
                    }
                }
                else
                {
                    unescapedString[writePosition++] = code;
                }
            }

            int length = unescapedString.Length - writePosition;
            if (length > 0)
            {
                unescapedString = unescapedString.Slice(0, writePosition);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UnescapeUtf8Hex(
            in byte a, in byte b, in byte c, in byte d,
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
            else if (unicodeDecimal >= 128 && unicodeDecimal <= 4063)
            {
                int bytesToShift = unicodeDecimal & _shiftBytesMask;
                unicodeDecimal -= bytesToShift;
                bytesToShift = bytesToShift << 2;
                unicodeDecimal += _utf8TwoByteMask + bytesToShift;

                unescapedString[writePosition++] = (byte)(unicodeDecimal >> 8);
                unescapedString[writePosition++] = (byte)unicodeDecimal;
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
