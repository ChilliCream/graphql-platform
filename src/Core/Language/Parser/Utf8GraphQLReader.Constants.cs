namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLReader
    {
        private static readonly bool[] _isControlCharacter = new bool[char.MaxValue + 1];
        private static readonly bool[] _isLetterOrUnderscore = new bool[256];
        private static readonly bool[] _isLetterOrDigitOrUnderscore = new bool[256];
        private static readonly bool[] _isEscapeCharacter = new bool[256];
        private static readonly bool[] _isPunctuator = new bool[256];
        private static readonly bool[] _isDigitOrMinus = new bool[256];
        private static readonly bool[] _isDigit = new bool[256];
        private static readonly byte[] _escapeCharacters = new byte[256];
        private static readonly bool[] _trimComment = new bool[256];
        private static readonly TokenKind[] _punctuatorKind = new TokenKind[256];
        private static readonly bool[] _isControlCharacterNoNewLine = new bool[256];

        public const byte A = (byte)'a';
        public const byte Z = (byte)'z';
        public const byte Hyphen = (byte)'-';
        public const byte Underscore = (byte)'_';
        public const byte Plus = (byte)'+';
        public const byte Minus = (byte)'-';
        public const byte Backslash = (byte)'\\';
        public const byte Forwardslash = (byte)'/';
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
        public const byte Comma = (byte)',';

        static Utf8GraphQLReader()
        {
            InitializeIsControlCharacterCache();
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

        private static void InitializeIsControlCharacterCache()
        {
            for (int i = 0; i <= 31; i++)
            {
                _isControlCharacterNoNewLine[i] = true;
                _isControlCharacter[i] = true;
            }

            _isControlCharacterNoNewLine[127] = true;
            _isControlCharacter[127] = true;

            _isControlCharacterNoNewLine[Tab] = false;
            _isControlCharacterNoNewLine[Return] = false;
            _isControlCharacterNoNewLine[NewLine] = false;
        }

        private static void InitializeIsEscapeCharacterCache()
        {
            _isEscapeCharacter[Quote] = true;
            _isEscapeCharacter[Forwardslash] = true;
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
                _escapeCharacters[i] = (byte)i;
            }

            _escapeCharacters[B] = Backspace;
            _escapeCharacters[F] = Formfeed;
            _escapeCharacters[N] = NewLine;
            _escapeCharacters[R] = Return;
            _escapeCharacters[T] = Tab;
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
            _trimComment[GraphQLConstants.Hash] = true;
            _trimComment[GraphQLConstants.Space] = true;
            _trimComment[GraphQLConstants.Tab] = true;
        }

        private static void InitializePunctuator()
        {
            _punctuatorKind[GraphQLConstants.Bang] = TokenKind.Bang;
            _punctuatorKind[GraphQLConstants.Dollar] = TokenKind.Dollar;
            _punctuatorKind[GraphQLConstants.Ampersand] = TokenKind.Ampersand;
            _punctuatorKind[GraphQLConstants.LeftParenthesis] = TokenKind.LeftParenthesis;
            _punctuatorKind[GraphQLConstants.RightParenthesis] = TokenKind.RightParenthesis;
            _punctuatorKind[GraphQLConstants.Colon] = TokenKind.Colon;
            _punctuatorKind[GraphQLConstants.Equal] = TokenKind.Equal;
            _punctuatorKind[GraphQLConstants.At] = TokenKind.At;
            _punctuatorKind[GraphQLConstants.LeftBracket] = TokenKind.LeftBracket;
            _punctuatorKind[GraphQLConstants.RightBracket] = TokenKind.RightBracket;
            _punctuatorKind[GraphQLConstants.LeftBrace] = TokenKind.LeftBrace;
            _punctuatorKind[GraphQLConstants.RightBrace] = TokenKind.RightBrace;
            _punctuatorKind[GraphQLConstants.Pipe] = TokenKind.Pipe;
        }
    }
}
