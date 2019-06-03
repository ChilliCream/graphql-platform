namespace HotChocolate.Language
{
    internal static partial class GraphQLConstants
    {
        static GraphQLConstants()
        {
            InitializeIsControlCharacterCache();
            InitializeIsEscapeCharacterCache();
            InitializeIsWhitespaceCache();
            InitializeIsPunctuatorCache();
            InitializeIsLetterOrUnderscoreCache();
            InitializeIsLetterOrDigitOUnderscoreCache();
            InitializeIsDigitOrMinusCache();
        }

        private static void InitializeIsControlCharacterCache()
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
        }

        private static void InitializeIsEscapeCharacterCache()
        {
            _isEscapeCharacter['"'] = true;
            _isEscapeCharacter['/'] = true;
            _isEscapeCharacter['\\'] = true;
            _isEscapeCharacter['b'] = true;
            _isEscapeCharacter['f'] = true;
            _isEscapeCharacter['n'] = true;
            _isEscapeCharacter['r'] = true;
            _isEscapeCharacter['t'] = true;
        }

        private static void InitializeIsWhitespaceCache()
        {
            _isWhitespace['\t'] = true;
            _isWhitespace['\r'] = true;
            _isWhitespace['\n'] = true;
            _isWhitespace[' '] = true;
            _isWhitespace[','] = true;
            _isWhitespace[0xfeff] = true;
        }

        private static void InitializeIsPunctuatorCache()
        {
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
        }

        private static void InitializeIsLetterOrUnderscoreCache()
        {
            for (char c = 'a'; c <= 'z'; c++)
            {
                _isLetterOrUnderscore[c] = true;
            }

            for (char c = 'A'; c <= 'Z'; c++)
            {
                _isLetterOrUnderscore[c] = true;
            }

            _isLetterOrUnderscore['_'] = true;
        }

        private static void InitializeIsLetterOrDigitOUnderscoreCache()
        {
            for (char c = 'a'; c <= 'z'; c++)
            {
                _isLetterOrDigitOrUnderscore[c] = true;
            }

            for (char c = 'A'; c <= 'Z'; c++)
            {
                _isLetterOrDigitOrUnderscore[c] = true;
            }

            _isLetterOrDigitOrUnderscore['0'] = true;
            _isLetterOrDigitOrUnderscore['1'] = true;
            _isLetterOrDigitOrUnderscore['2'] = true;
            _isLetterOrDigitOrUnderscore['3'] = true;
            _isLetterOrDigitOrUnderscore['4'] = true;
            _isLetterOrDigitOrUnderscore['5'] = true;
            _isLetterOrDigitOrUnderscore['6'] = true;
            _isLetterOrDigitOrUnderscore['7'] = true;
            _isLetterOrDigitOrUnderscore['8'] = true;
            _isLetterOrDigitOrUnderscore['9'] = true;

            _isLetterOrDigitOrUnderscore['_'] = true;
        }

        private static void InitializeIsDigitOrMinusCache()
        {
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
    }
}
