namespace HotChocolate.Utilities
{
    /// <summary>
    /// This class provides internal char utilities
    /// that are used to tokenize a GraphQL source text.
    /// These utilities are used by the lexer dfault implementation.
    /// </summary>
    internal static partial class CharExtensions2
    {
        private static readonly bool[] _isLetterOrUnderscore =
            new bool[char.MaxValue + 1];
        private static readonly bool[] _isControlCharacter =
            new bool[char.MaxValue + 1];
        private static readonly bool[] _isEscapeCharacter =
            new bool[char.MaxValue + 1];
        private static readonly bool[] _isWhitespace =
            new bool[char.MaxValue + 1];
        private static readonly bool[] _isPunctuator =
            new bool[char.MaxValue + 1];
        private static readonly bool[] _isDigitOrMinus =
            new bool[char.MaxValue + 1];
        private const byte _a = (byte)'a';
        private const byte _z = (byte)'z';
        private const byte _dot = (byte)'.';
        private const byte _hyphen = (byte)'-';
        private const byte _underscore = (byte)'_';
        private const byte _plus = (byte)'+';
        private const byte _quote = (byte)'"';
        private const byte _backslash = (byte)'\\';
        private const byte _hash = (byte)'#';
        private const byte _newLine = (byte)'\n';
        private const byte _return = (byte)'\r';


        public static bool IsLetterOrDigitOrUnderscore(in this byte c)
        {
            return c.IsLetterOrUnderscore() || c.IsDigit();
        }

        public static bool IsLetter(in this byte c)
        {
            byte normalized = (byte)(c | 0x20);
            return (normalized >= _a && normalized <= _z);
        }

        public static ref readonly bool IsLetterOrUnderscore(in this byte c)
        {
            return ref _isLetterOrUnderscore[c];
        }

        public static bool IsDigit(in this byte c)
        {
            return c >= 48 && c <= 57;
        }

        public static ref bool IsDigitOrMinus(in this byte c)
        {
            return ref _isDigitOrMinus[c];
        }

        public static bool IsDot(in this byte c)
        {
            return c == _dot;
        }

        public static bool IsHyphen(in this byte c)
        {
            return c == _hyphen;
        }

        public static bool IsUnderscore(in this byte c)
        {
            return c == _underscore;
        }

        public static bool IsMinus(in this byte c)
        {
            return c.IsHyphen();
        }

        public static bool IsPlus(in this byte c)
        {
            return c == _plus;
        }

        public static bool IsQuote(in this byte c)
        {
            return c == _quote;
        }

        public static bool IsBackslash(in this byte c)
        {
            return c == _backslash;
        }

        public static bool IsHash(in this byte c)
        {
            return c == _hash;
        }

        public static ref readonly bool IsPunctuator(in this byte c)
        {
            return ref _isPunctuator[c];
        }

        public static ref readonly bool IsWhitespace(in this byte c)
        {
            return ref _isWhitespace[c];
        }

        public static bool IsNewLine(in this byte c)
        {
            // 0x000a
            return c == _newLine;
        }

        public static bool IsReturn(in this byte c)
        {
            // 0x000d
            return c == _return;
        }

        public static ref readonly bool IsValidEscapeCharacter(in this char c)
        {
            return ref _isEscapeCharacter[c];
        }

        public static char EscapeCharacter(in this char c)
        {
            switch (c)
            {
                case 'b':
                    return '\b';
                case 'f':
                    return '\f';
                case 'n':
                    return '\n';
                case 'r':
                    return '\r';
                case 't':
                    return '\t';
                default:
                    return c;
            }
        }

        public static ref readonly bool IsControlCharacter(in this char c)
        {
            return ref _isControlCharacter[c];
        }
    }
}
