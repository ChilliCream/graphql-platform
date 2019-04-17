namespace HotChocolate.Language
{
    /// <summary>
    /// This class provides internal char utilities
    /// that are used to tokenize a GraphQL source text.
    /// These utilities are used by the lexer dfault implementation.
    /// </summary>
    internal static partial class ReaderHelper
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
        private const byte _hyphen = (byte)'-';
        private const byte _underscore = (byte)'_';
        private const byte _plus = (byte)'+';
        private const byte _quote = (byte)'"';
        private const byte _backslash = (byte)'\\';
        private const byte _hash = (byte)'#';
        private const byte _newLine = (byte)'\n';
        private const byte _return = (byte)'\r';
        private const byte _b = (byte)'b';
        private const byte _besc = (byte)'\b';
        private const byte _f = (byte)'f';
        private const byte _fesc = (byte)'\f';
        private const byte _n = (byte)'n';
        private const byte _r = (byte)'r';
        private const byte _t = (byte)'t';
        private const byte _tab = (byte)'\t';

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


        public static bool IsLetterOrDigitOrUnderscore(in this byte c)
        {
            return IsLetterOrUnderscore(in c) || IsDigit(in c);
        }

        public static bool IsLetter(in this byte c)
        {
            byte normalized = (byte)(c | 0x20);
            return (normalized >= _a && normalized <= _z);
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

        public static bool IsDot(in this byte c)
        {
            return c == Dot;
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
            return IsHyphen(in c);
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

        public static bool IsPunctuator(in this byte c)
        {
            return _isPunctuator[c];
        }

        public static bool IsWhitespace(in this byte c)
        {
            return _isWhitespace[c];
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

        public static bool IsValidEscapeCharacter(in this byte c)
        {
            return _isEscapeCharacter[c];
        }

        public static byte EscapeCharacter(in this byte c)
        {
            switch (c)
            {
                case _b:
                    return _besc;
                case _f:
                    return _fesc;
                case _n:
                    return _newLine;
                case _r:
                    return _return;
                case _t:
                    return _tab;
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
