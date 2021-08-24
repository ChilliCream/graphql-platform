namespace HotChocolate.Language
{
    internal static partial class GraphQLConstants
    {
        static GraphQLConstants()
        {
            InitializeIsEscapeCharacterCache();
            InitializeEscapeCharacterCache();
            InitializeIsPunctuatorCache();
            InitializeIsLetterOrUnderscoreCache();
            InitializeIsLetterOrDigitOUnderscoreCache();
            InitializeIsDigitCache();
            InitializeIsDigitOrMinusCache();
            InitializeTrimComment();
            InitializePunctuator();
        }

        private static void InitializeIsEscapeCharacterCache()
        {
            _isEscapeCharacter[Quote] = true;
            _isEscapeCharacter[ForwardSlash] = true;
            _isEscapeCharacter[Backslash] = true;
            _isEscapeCharacter[B] = true;
            _isEscapeCharacter[F] = true;
            _isEscapeCharacter[N] = true;
            _isEscapeCharacter[R] = true;
            _isEscapeCharacter[T] = true;
            _isEscapeCharacter[U] = true;
        }

        private static void InitializeEscapeCharacterCache()
        {
            for (int i = byte.MinValue; i <= byte.MaxValue; i++)
            {
                var c = (char)i;
                _escapeCharacters[c] = (byte)c;
            }

            _escapeCharacters[B] = Backspace;
            _escapeCharacters[F] = FormFeed;
            _escapeCharacters[N] = LineFeed;
            _escapeCharacters[R] = Return;
            _escapeCharacters[T] = HorizontalTab;
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
            for (var c = 'a'; c <= 'z'; c++)
            {
                _isLetterOrUnderscore[c] = true;
            }

            for (var c = 'A'; c <= 'Z'; c++)
            {
                _isLetterOrUnderscore[c] = true;
            }

            _isLetterOrUnderscore['_'] = true;
        }

        private static void InitializeIsLetterOrDigitOUnderscoreCache()
        {
            for (var c = 'a'; c <= 'z'; c++)
            {
                _isLetterOrDigitOrUnderscore[c] = true;
            }

            for (var c = 'A'; c <= 'Z'; c++)
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

        private static void InitializeIsDigitCache()
        {
            _isDigit['0'] = true;
            _isDigit['1'] = true;
            _isDigit['2'] = true;
            _isDigit['3'] = true;
            _isDigit['4'] = true;
            _isDigit['5'] = true;
            _isDigit['6'] = true;
            _isDigit['7'] = true;
            _isDigit['8'] = true;
            _isDigit['9'] = true;
        }

        private static void InitializeTrimComment()
        {
            _trimComment[Hash] = true;
            _trimComment[Space] = true;
            _trimComment[HorizontalTab] = true;
        }

        private static void InitializePunctuator()
        {
            _punctuatorKind[Bang] = TokenKind.Bang;
            _punctuatorKind[Dollar] = TokenKind.Dollar;
            _punctuatorKind[Ampersand] = TokenKind.Ampersand;
            _punctuatorKind[LeftParenthesis] = TokenKind.LeftParenthesis;
            _punctuatorKind[RightParenthesis] = TokenKind.RightParenthesis;
            _punctuatorKind[Colon] = TokenKind.Colon;
            _punctuatorKind[Equal] = TokenKind.Equal;
            _punctuatorKind[At] = TokenKind.At;
            _punctuatorKind[LeftBracket] = TokenKind.LeftBracket;
            _punctuatorKind[RightBracket] = TokenKind.RightBracket;
            _punctuatorKind[LeftBrace] = TokenKind.LeftBrace;
            _punctuatorKind[RightBrace] = TokenKind.RightBrace;
            _punctuatorKind[Pipe] = TokenKind.Pipe;
        }
    }
}
