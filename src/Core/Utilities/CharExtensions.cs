namespace HotChocolate.Utilities
{
    /// <summary>
    /// This class provides internal char utilities
    /// that are used to tokenize a GraphQL source text.
    /// These utilities are used by the lexer dfault implementation.
    /// </summary>
    internal static partial class CharExtensions
    {
        private static readonly bool[] _isLetterOrUnderscore = new bool[char.MaxValue + 1];
        private static readonly bool[] _isControlCharacter = new bool[char.MaxValue + 1];
        private static readonly bool[] _isEscapeCharacter = new bool[char.MaxValue + 1];
        private static readonly bool[] _isWhitespace = new bool[char.MaxValue + 1];
        private static readonly bool[] _isPunctuator = new bool[char.MaxValue + 1];
        private static readonly bool[] _isDigitOrMinus = new bool[char.MaxValue + 1];

        public static bool IsLetterOrDigitOrUnderscore(in this char c)
        {
            return c.IsLetterOrUnderscore() || c.IsDigit();
        }

        public static bool IsLetter(in this char c)
        {
            char normalized = (char)(c | 0x20);
            return (normalized >= 'a' && normalized <= 'z');
        }

        public static ref readonly bool IsLetterOrUnderscore(in this char c)
        {
            return ref _isLetterOrUnderscore[c];
        }

        public static bool IsDigit(in this char c)
        {
            return c >= 48 && c <= 57;
        }

        public static ref bool IsDigitOrMinus(in this char c)
        {
            return ref _isDigitOrMinus[c];
        }

        public static bool IsDot(in this char c)
        {
            return c == '.';
        }

        public static bool IsHyphen(in this char c)
        {
            return c == '-';
        }

        public static bool IsUnderscore(in this char c)
        {
            return c == '_';
        }

        public static bool IsMinus(in this char c)
        {
            return c.IsHyphen();
        }

        public static bool IsPlus(in this char c)
        {
            return c == '+';
        }

        public static bool IsQuote(in this char c)
        {
            return c == '"';
        }

        public static bool IsBackslash(in this char c)
        {
            return c == '\\';
        }

        public static bool IsHash(in this char c)
        {
            return c == '#';
        }

        public static ref readonly bool IsPunctuator(in this char c)
        {
            return ref _isPunctuator[c];
        }

        public static ref readonly bool IsWhitespace(in this char c)
        {
            return ref _isWhitespace[c];
        }

        public static bool IsNewLine(in this char c)
        {
            // 0x000a
            return c == '\n';
        }

        public static bool IsReturn(in this char c)
        {
            // 0x000d
            return c == '\r';
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
