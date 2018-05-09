namespace HotChocolate.Language
{
    /// <summary>
    /// This class provides internal char utilities
    /// that are used to tokenize a GraphQL source text.
    /// These utilities are used by the lexer dfault implementation.
    /// </summary>
    internal static class CharExtensions
    {
        private static readonly bool[] _isLetterOrUnderscore = new bool[char.MaxValue + 1];
        private static readonly bool[] _isControlCharacter = new bool[char.MaxValue + 1];
        private static readonly bool[] _isEscapeCharacter = new bool[char.MaxValue + 1];
        private static readonly bool[] _isWhitespace = new bool[char.MaxValue + 1];
        private static readonly bool[] _isPunctuator = new bool[char.MaxValue + 1];
        private static readonly bool[] _isDigitOrMinus = new bool[char.MaxValue + 1];

        #region Initialize Arrays

        static CharExtensions()
        {
            for (int i = 0; i < 9; i++)
            {
                _isControlCharacter[i] = true;
            }
            for (int i = 10; i <= 31; i++)
            {
                _isControlCharacter[i] = true;
            }
            _isControlCharacter[127] = true;

            _isEscapeCharacter['"'] = true;
            _isEscapeCharacter['/'] = true;
            _isEscapeCharacter['\\'] = true;
            _isEscapeCharacter['\b'] = true;
            _isEscapeCharacter['\f'] = true;
            _isEscapeCharacter['\n'] = true;
            _isEscapeCharacter['\r'] = true;
            _isEscapeCharacter['\t'] = true;

            _isWhitespace['\t'] = true;
            _isWhitespace['\r'] = true;
            _isWhitespace['\n'] = true;
            _isWhitespace[' '] = true;
            _isWhitespace[','] = true;
            _isWhitespace[0xfeff] = true;

            _isPunctuator['!'] = true;
            _isPunctuator['$'] = true;
            _isPunctuator['&'] = true;
            _isPunctuator['('] = true;
            _isPunctuator[')'] = true;
            _isPunctuator[':'] = true;
            _isPunctuator['='] = true;
            _isPunctuator['@'] = true;
            _isPunctuator['['] = true;
            _isPunctuator[']'] = true;
            _isPunctuator['{'] = true;
            _isPunctuator['|'] = true;
            _isPunctuator['}'] = true;
            _isPunctuator['.'] = true;

            for (char c = 'a'; c <= 'z'; c++)
            {
                _isLetterOrUnderscore[c] = true;
            }

            for (char c = 'A'; c <= 'Z'; c++)
            {
                _isLetterOrUnderscore[c] = true;
            }

            _isLetterOrUnderscore['_'] = true;

            _isDigitOrMinus['-'] = true;
            _isDigitOrMinus['0'] = true;
            _isDigitOrMinus['1'] = true;
            _isDigitOrMinus['2'] = true;
            _isDigitOrMinus['3'] = true;
            _isDigitOrMinus['4'] = true;
            _isDigitOrMinus['5'] = true;
            _isDigitOrMinus['6'] = true;
            _isDigitOrMinus['7'] = true;
            _isDigitOrMinus['8'] = true;
            _isDigitOrMinus['9'] = true;
        }

        #endregion

        public static bool IsLetterOrDigitOrUnderscore(in this char c)
        {
            return c.IsLetterOrUnderscore() || c.IsDigit();
        }

        public static bool IsLetter(in this char c)
        {
            char normalized = (char)(c | 0x20);
            return ((normalized >= 'a' && normalized <= 'z'));
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

        public static ref readonly bool IsControlCharacter(in this char c)
        {
            return ref _isControlCharacter[c];
        }
    }
}
