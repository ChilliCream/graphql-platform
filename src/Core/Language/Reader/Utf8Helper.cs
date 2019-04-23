namespace HotChocolate.Language
{
    public static class Utf8Helper
    {
        private const int _utf8TwoByteMask = 0b1100_0000_1000_0000;
        private const int _shiftBytesMask = 0b1111_1111_1100_0000;

        public static int Unescape(
            in ReadOnlySpan<byte> escapedString,
            ref Span<byte> unescapedString)
        {
            int readPosition = 0;
            int writePosition = 0;
            ref readonly byte code = ref escapedString[readPosition];

            while (readPosition < escapedString.Length)
            {
                if (ReaderHelper.IsBackslash(in code))
                {
                    ref readonly byte code = ref escapedString[++readPosition];
                    if (ReaderHelper.IsValidEscapeCharacter(code))
                    {
                        unescapedString[writePosition++] =
                            ReaderHelper.EscapeCharacter(in code);
                    }
                    else if (code == ReaderHelper.U)
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
                        // TODO : Syntax Exception
                        throw new Exception();
                    }
                }
                else
                {
                    unescapedString[writePosition++] = code;
                }
            }

            return writePosition;
        }

        private static void UnescapeUtf8Hex(
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
                unescapedString[writePosition++] = (byte)data;
            }
            else if (unicodeDecimal >= 128 && unicodeDecimal <= 4063)
            {
                int bytesToShift = unicodeDecimal & _shiftBytesMask;
                unicodeDecimal -= bytesToShift;
                bytesToShift = bytesToShift << 2;
                unicodeDecimal += _utf8TwoByteMask + bytesToShift;

                unescapedString[writePosition++][0] = (byte)(unicodeDecimal >> 8);
                unescapedString[writePosition++][1] = (byte)unicodeDecimal;
            }

            return unicodeDecimal;
        }

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
