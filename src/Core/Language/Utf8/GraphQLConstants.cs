namespace HotChocolate.Language
{
    /// <summary>
    /// This class provides internal char utilities
    /// that are used to tokenize a GraphQL source text.
    /// These utilities are used by the lexer dfault implementation.
    /// </summary>
    internal static partial class GraphQLConstants
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

        public const int StackallocThreshold = 256;

        public const byte A = (byte)'a';
        public const byte Z = (byte)'z';
        public const byte Hyphen = (byte)'-';
        public const byte Underscore = (byte)'_';
        public const byte Plus = (byte)'+';
        public const byte Minus = (byte)'-';
        public const byte Backslash = (byte)'\\';
        public const byte B = (byte)'b';
        public const byte Backspace = (byte)'\b';
        public const byte F = (byte)'f';
        public const byte Formfeed = (byte)'\f';
        public const byte N = (byte)'n';
        public const byte R = (byte)'r';
        public const byte T = (byte)'t';

        public const byte Bang = (byte)'!';
        public const byte Dollar = (byte)'$';
        public const byte Ampersand = (byte)'&';
        public const byte LeftParenthesis = (byte)'(';
        public const byte RightParenthesis = (byte)')';
        public const byte Colon = (byte)':';
        public const byte Equal = (byte)'=';
        public const byte At = (byte)'@';
        public const byte LeftBracket = (byte)'[';
        public const byte RightBracket = (byte)']';
        public const byte LeftBrace = (byte)'{';
        public const byte RightBrace = (byte)'}';
        public const byte Pipe = (byte)'|';
        public const byte Dot = (byte)'.';
        public const byte Space = (byte)' ';
        public const byte Hash = (byte)'#';
        public const byte Tab = (byte)'\t';
        public const byte U = (byte)'u';
        public const byte Zero = (byte)'0';
        public const byte E = (byte)'e';
        public const byte NewLine = (byte)'\n';
        public const byte Return = (byte)'\r';
        public const byte Quote = (byte)'"';

        public static bool IsLetterOrDigitOrUnderscore(in this byte c)
        {
            return IsLetterOrUnderscore(in c) || IsDigit(in c);
        }

        public static bool IsLetter(in this byte c)
        {
            byte normalized = (byte)(c | 0x20);
            return (normalized >= A && normalized <= Z);
        }

        public static bool IsLetterOrUnderscore(in this byte c)
        {
            return _isLetterOrUnderscore[c];
        }

        public static bool IsDigit(in this byte c)
        {
            return c >= 48 && c <= 57;
        }

        public static bool IsDigitOrMinus(in this byte c)
        {
            return _isDigitOrMinus[c];
        }

        public static bool IsPunctuator(in this byte c)
        {
            return _isPunctuator[c];
        }

        public static bool IsWhitespace(in this byte c)
        {
            return _isWhitespace[c];
        }

        public static bool IsValidEscapeCharacter(in this byte c)
        {
            return _isEscapeCharacter[c];
        }

        public static byte EscapeCharacter(in this byte c)
        {
            switch (c)
            {
                case B:
                    return Backspace;
                case F:
                    return Formfeed;
                case N:
                    return NewLine;
                case R:
                    return Return;
                case T:
                    return Tab;
                default:
                    return c;
            }
        }

        public static bool IsControlCharacter(in this byte c)
        {
            return _isControlCharacter[c];
        }
    }
}
