using System.Collections.Generic;
using System.Xml.Schema;
using System.Buffers;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLReader
    {
        private static readonly UTF8Encoding _utf8Encoding = new UTF8Encoding();
        private static readonly byte _space = (byte)' ';
        private int _nextNewLines;
        private ReadOnlySpan<byte> _value;

        public Utf8GraphQLReader(ReadOnlySpan<byte> graphQLData)
        {
            GraphQLData = graphQLData;
            Kind = TokenKind.StartOfFile;
            Start = 0;
            End = 0;
            LineStart = 0;
            Position = 0;
            Line = 1;
            Column = 1;
            _value = null;
            _nextNewLines = 0;
        }

        public ReadOnlySpan<byte> GraphQLData { get; }

        /// <summary>
        /// Gets the kind of <see cref="SyntaxToken" />.
        /// </summary>
        public TokenKind Kind { get; private set; }

        /// <summary>
        /// Gets the character offset at which this node begins.
        /// </summary>
        public int Start { get; private set; }

        /// <summary>
        /// Gets the character offset at which this node ends.
        /// </summary>
        public int End { get; private set; }

        /// <summary>
        /// The current position of the lexer pointer.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Gets the 1-indexed line number on which this
        /// <see cref="SyntaxToken" /> appears.
        /// </summary>
        public int Line { get; private set; }

        /// <summary>
        /// The source index of where the current line starts.
        /// </summary>
        public int LineStart { get; private set; }

        /// <summary>
        /// Gets the 1-indexed column number at which this
        /// <see cref="SyntaxToken" /> begins.
        /// </summary>
        public int Column { get; private set; }

        /// <summary>
        /// For non-punctuation tokens, represents the interpreted
        /// value of the token.
        /// </summary>
        public ReadOnlySpan<byte> Value => _value;

        public unsafe string GetString()
        {
            bool isBlockString = Kind == TokenKind.BlockString;

            int length = checked((int)_value.Length);
            bool useStackalloc =
                _value.Length <= GraphQLConstants.StackallocThreshold;

            byte[] escapedArray = null;
            byte[] unescapedArray = null;

            Span<byte> escapedSpan = useStackalloc
                ? stackalloc byte[_value.Length]
                : (escapedArray = ArrayPool<byte>.Shared.Rent(_value.Length));

            Span<byte> unescapedSpan = useStackalloc
                ? stackalloc byte[_value.Length]
                : (unescapedArray = ArrayPool<byte>.Shared.Rent(_value.Length));

            try
            {
                _value.CopyTo(escapedSpan);
                escapedSpan = escapedSpan.Slice(0, length);

                UnescapeValue(escapedSpan, ref unescapedSpan, isBlockString);

                fixed (byte* bytePtr = unescapedSpan)
                {
                    return _utf8Encoding.GetString(
                        bytePtr,
                        unescapedSpan.Length);
                }
            }
            finally
            {
                if (escapedArray != null)
                {
                    escapedSpan.Clear();
                    unescapedSpan.Clear();

                    ArrayPool<byte>.Shared.Return(escapedArray);
                    ArrayPool<byte>.Shared.Return(unescapedArray);
                }
            }
        }

        public unsafe string GetString(ReadOnlySpan<byte> unescapedValue)
        {
            fixed (byte* bytePtr = unescapedValue)
            {
                return _utf8Encoding.GetString(bytePtr, _value.Length);
            }
        }

        public string GetComment()
        {
            if (_value.Length > 0)
            {
                StringHelper.TrimStringToken(ref _value);
            }
            return GetString(_value);
        }

        public string GetName() => GetString(_value);
        public string GetScalarValue() => GetString(_value);

        private static void UnescapeValue(
            in ReadOnlySpan<byte> escaped,
            ref Span<byte> unescapedValue,
            bool isBlockString)
        {
            Utf8Helper.Unescape(
                in escaped,
                ref unescapedValue,
                isBlockString);

            if (isBlockString)
            {
                StringHelper.TrimBlockStringToken(
                    unescapedValue, ref unescapedValue);
            }
        }

        public void UnescapeValue(ref Span<byte> unescapedValue)
        {
            UnescapeValue(
                in _value,
                ref unescapedValue,
                Kind == TokenKind.BlockString);
        }

        public bool Read()
        {
            SkipWhitespaces();
            UpdateColumn();

            if (IsEndOfStream())
            {
                Start = Position;
                End = Position;
                Kind = TokenKind.EndOfFile;
                _value = null;
                return false;
            }

            ref readonly byte code = ref GraphQLData[Position];

            if (GraphQLConstants.IsLetterOrUnderscore(in code))
            {
                ReadNameToken();
                return true;
            }

            if (GraphQLConstants.IsPunctuator(in code))
            {
                ReadPunctuatorToken(in code);
                return true;
            }

            if (GraphQLConstants.IsDigitOrMinus(in code))
            {
                ReadNumberToken(in code);
                return true;
            }

            if (code == GraphQLConstants.Hash)
            {
                ReadCommentToken();
                return true;
            }

            if (code == GraphQLConstants.Quote)
            {
                if (GraphQLData[Position + 1] == GraphQLConstants.Quote
                    && GraphQLData[Position + 2] == GraphQLConstants.Quote)
                {
                    Position += 2;
                    ReadBlockStringToken();
                }
                else
                {
                    ReadStringValueToken();
                }
                return true;
            }

            throw new SyntaxException(this,
                "Unexpected character.");
        }

        /// <summary>
        /// Reads name tokens as specified in
        /// http://facebook.github.io/graphql/October2016/#Name
        /// [_A-Za-z][_0-9A-Za-z]
        /// from the current lexer state.
        /// </summary>
        /// <param name="state">The lexer state.</param>
        /// <param name="previous">The previous-token.</param>
        /// <returns>
        /// Returns the name token read from the current lexer state.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadNameToken()
        {
            var start = Position;
            var position = Position;

            do
            {
                position++;
            }
            while (position < GraphQLData.Length
                && GraphQLConstants.IsLetterOrDigitOrUnderscore(
                    in GraphQLData[position]));

            Kind = TokenKind.Name;
            Start = start;
            End = position;
            _value = GraphQLData.Slice(start, position - start);
            Position = position;
        }

        /// <summary>
        /// Reads punctuator tokens as specified in
        /// http://facebook.github.io/graphql/October2016/#sec-Punctuators
        /// one of ! $ ( ) ... : = @ [ ] { | }
        /// additionaly the reader will tokenize ampersands.
        /// </summary>
        /// <param name="state">
        /// The lexer state.
        /// </param>
        /// <param name="previous">
        /// The previous-token.
        /// </param>
        /// <param name="firstCode">
        /// The first character of the punctuator.
        /// </param>
        /// <returns>
        /// Returns the punctuator token read from the current lexer state.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadPunctuatorToken(in byte code)
        {
            Start = Position;
            End = ++Position;
            _value = null;

            switch (code)
            {
                case GraphQLConstants.Bang:
                    Kind = TokenKind.Bang;
                    break;

                case GraphQLConstants.Dollar:
                    Kind = TokenKind.Dollar;
                    break;

                case GraphQLConstants.Ampersand:
                    Kind = TokenKind.Ampersand;
                    break;

                case GraphQLConstants.LeftParenthesis:
                    Kind = TokenKind.LeftParenthesis;
                    break;

                case GraphQLConstants.RightParenthesis:
                    Kind = TokenKind.RightParenthesis;
                    break;

                case GraphQLConstants.Colon:
                    Kind = TokenKind.Colon;
                    break;

                case GraphQLConstants.Equal:
                    Kind = TokenKind.Equal;
                    break;

                case GraphQLConstants.At:
                    Kind = TokenKind.At;
                    break;

                case GraphQLConstants.LeftBracket:
                    Kind = TokenKind.LeftBracket;
                    break;

                case GraphQLConstants.RightBracket:
                    Kind = TokenKind.RightBracket;
                    break;

                case GraphQLConstants.LeftBrace:
                    Kind = TokenKind.LeftBrace;
                    break;

                case GraphQLConstants.RightBrace:
                    Kind = TokenKind.RightBrace;
                    break;

                case GraphQLConstants.Pipe:
                    Kind = TokenKind.Pipe;
                    break;

                case GraphQLConstants.Dot:
                    if (GraphQLData[Position] == GraphQLConstants.Dot
                        && GraphQLData[Position + 1] == GraphQLConstants.Dot)
                    {
                        Position += 2;
                        End = Position;
                        Kind = TokenKind.Spread;
                    }
                    else
                    {
                        // TODO : exception
                        Position--;
                        throw new SyntaxException((LexerState)null,
                            "Expected a spread token.");
                    }
                    break;

                default:
                    Position--;
                    throw new SyntaxException((LexerState)null,
                        "Unexpected punctuator character.");
            }
        }

        /// <summary>
        /// Reads int tokens as specified in
        /// http://facebook.github.io/graphql/October2016/#IntValue
        /// or a float tokens as specified in
        /// http://facebook.github.io/graphql/October2016/#FloatValue
        /// from the current lexer state.
        /// </summary>
        /// <param name="state">The lexer state.</param>
        /// <param name="previous">The previous-token.</param>
        /// <param name="firstCode">
        /// The first character of the int or float token.
        /// </param>
        /// <returns>
        /// Returns the int or float tokens read from the current lexer state.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadNumberToken(
            in byte firstCode)
        {
            int start = Position;
            ref readonly byte code = ref firstCode;
            var isFloat = false;

            if (code == GraphQLConstants.Minus)
            {
                code = ref GraphQLData[++Position];
            }

            if (code == GraphQLConstants.Zero)
            {
                code = ref GraphQLData[++Position];
                if (GraphQLConstants.IsDigit(in code))
                {
                    throw new SyntaxException(this,
                        $"Invalid number, unexpected digit after 0: {code}.");
                }
            }
            else
            {
                ReadDigits(in code);
                if (Position < GraphQLData.Length)
                {
                    code = ref GraphQLData[Position];
                }
                else
                {
                    code = ref _space;
                }
            }

            if (code == GraphQLConstants.Dot)
            {
                isFloat = true;
                code = ref GraphQLData[++Position];
                ReadDigits(in code);
                if (Position < GraphQLData.Length)
                {
                    code = ref GraphQLData[Position];
                }
                else
                {
                    code = ref _space;
                }
            }

            if ((code | (char)0x20) == GraphQLConstants.E)
            {
                isFloat = true;
                code = ref GraphQLData[++Position];
                if (code == GraphQLConstants.Plus
                    || code == GraphQLConstants.Minus)
                {
                    code = ref GraphQLData[++Position];
                }
                ReadDigits(in code);
            }

            Kind = isFloat ? TokenKind.Float : TokenKind.Integer;
            Start = start;
            End = Position;
            _value = GraphQLData.Slice(start, Position - start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadDigits(in byte firstCode)
        {
            if (!firstCode.IsDigit())
            {
                throw new SyntaxException(this,
                    $"Invalid number, expected digit but got: {firstCode}.");
            }

            while (++Position < GraphQLData.Length
                && GraphQLConstants.IsDigit(GraphQLData[Position]))
            { }
        }

        /// <summary>
        /// Reads comment tokens as specified in
        /// http://facebook.github.io/graphql/October2016/#sec-Comments
        /// #[\u0009\u0020-\uFFFF]*
        /// from the current lexer state.
        /// </summary>
        /// <param name="state">The lexer state.</param>
        /// <param name="previous">The previous-token.</param>
        /// <returns>
        /// Returns the comment token read from the current lexer state.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadCommentToken()
        {
            var start = Position;
            var trimStart = Position;
            bool trim = true;

            while (++Position < GraphQLData.Length
                && !GraphQLConstants.IsControlCharacter(
                    in GraphQLData[Position]))
            {
                if (trim)
                {
                    switch (GraphQLData[Position])
                    {
                        case GraphQLConstants.Hash:
                        case GraphQLConstants.Space:
                        case GraphQLConstants.Tab:
                            trimStart = Position;
                            break;

                        default:
                            trim = false;
                            break;
                    }
                }
            }

            Kind = TokenKind.Comment;
            Start = start;
            End = Position;
            _value = GraphQLData.Slice(trimStart, Position - trimStart);
        }

        /// <summary>
        /// Reads string tokens as specified in
        /// http://facebook.github.io/graphql/October2016/#StringValue
        /// "([^"\\\u000A\u000D]|(\\(u[0-9a-fA-F]{4}|["\\/bfnrt])))*"
        /// from the current lexer state.
        /// </summary>
        /// <param name="state">The lexer state.</param>
        /// <param name="previous">The previous-token.</param>
        /// <returns>
        /// Returns the string value token read from the current lexer state.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadStringValueToken()
        {
            var start = Position;
            var value = new StringBuilder();

            ref readonly byte code = ref GraphQLData[++Position];

            while (code != GraphQLConstants.NewLine
                && code != GraphQLConstants.Return)
            {
                // closing Quote (")
                if (code == GraphQLConstants.Quote)
                {
                    Kind = TokenKind.String;
                    Start = start;
                    End = Position;
                    _value = GraphQLData.Slice(start + 1, Position - start - 1);
                    Position++;
                    return;
                }

                // SourceCharacter
                if (GraphQLConstants.IsControlCharacter(in code))
                {
                    throw new SyntaxException(this,
                        $"Invalid character within String: {code}.");
                }

                if (code == GraphQLConstants.Backslash)
                {
                    code = ref GraphQLData[++Position];
                    if (!GraphQLConstants.IsValidEscapeCharacter(in code))
                    {
                        throw new SyntaxException(this,
                            $"Invalid character escape sequence: \\{code}.");
                    }
                }

                code = ref GraphQLData[++Position];
            }

            throw new SyntaxException(this, "Unterminated string.");
        }

        /// <summary>
        /// Reads block string tokens as specified in
        /// http://facebook.github.io/graphql/draft/#BlockStringCharacter
        /// from the current lexer state.
        /// </summary>
        /// <param name="state">The lexer state.</param>
        /// <param name="previous">The previous-token.</param>
        /// <returns>
        /// Returns the block string token read from the current lexer state.
        /// </returns>
        private void ReadBlockStringToken()
        {
            var start = Position - 2;

            ref readonly byte code = ref GraphQLData[++Position];

            while (!IsEndOfStream())
            {
                // Closing Triple-Quote (""")
                if (code == GraphQLConstants.Quote
                    && GraphQLData[Position + 1] == GraphQLConstants.Quote
                    && GraphQLData[Position + 2] == GraphQLConstants.Quote)
                {
                    Kind = TokenKind.BlockString;
                    Start = start;
                    End = Position + 2;
                    _value = GraphQLData.Slice(start + 3, Position - start - 3);

                    int newLines = StringHelper.CountLines(in _value) - 1;
                    if (newLines > 0)
                    {
                        _nextNewLines = newLines;
                    }

                    Position = End + 1;
                    return;
                }

                // SourceCharacter
                if (code.IsControlCharacter()
                    && code != GraphQLConstants.NewLine
                    && code != GraphQLConstants.Return)
                {
                    throw new SyntaxException(this,
                        $"Invalid character within String: ${code}.");
                }

                if (code == GraphQLConstants.Backslash
                    && GraphQLData[Position + 1] == GraphQLConstants.Quote
                    && GraphQLData[Position + 2] == GraphQLConstants.Quote
                    && GraphQLData[Position + 3] == GraphQLConstants.Quote)
                {
                    Position += 3;
                }

                code = ref GraphQLData[++Position];
            }

            throw new SyntaxException(this, "Unterminated string.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SkipWhitespaces()
        {
            if (IsEndOfStream())
            {
                return;
            }

            if (_nextNewLines > 0)
            {
                NewLine(_nextNewLines);
                _nextNewLines = 0;
            }

            ref readonly byte code = ref GraphQLData[Position];

            while (GraphQLConstants.IsWhitespace(in code))
            {
                if (code == GraphQLConstants.NewLine)
                {
                    int next = Position + 1;
                    if (next < GraphQLData.Length
                        && GraphQLData[next] == GraphQLConstants.Return)
                    {
                        Position = next;
                    }
                    NewLine();
                }
                else if (code == GraphQLConstants.Return)
                {
                    int next = Position + 1;
                    if (next < GraphQLData.Length
                        && GraphQLData[next] == GraphQLConstants.NewLine)
                    {
                        Position = next;
                    }
                    NewLine();
                }

                Position++;

                if (IsEndOfStream())
                {
                    return;
                }

                code = ref GraphQLData[Position];
            }
        }

        /// <summary>
        /// Sets the state to a new line.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NewLine()
        {
            Line++;
            LineStart = Position;
            UpdateColumn();
        }

        /// <summary>
        /// Sets the state to a new line.
        /// </summary>
        /// <param name="lines">
        /// The number of lines to skip.
        /// </param>
        public void NewLine(int lines)
        {
            if (lines < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(lines),
                    "Must be greater or equal to 1.");
            }

            Line += lines;
            LineStart = Position;
            UpdateColumn();
        }

        /// <summary>
        /// Updates the column index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateColumn()
        {
            Column = 1 + Position - LineStart;
        }

        /// <summary>
        /// Checks if the lexer source pointer has reached
        /// the end of the GraphQL source text.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEndOfStream()
        {
            return Position >= GraphQLData.Length;
        }

        public unsafe static int ConvertToBytes(string s, byte[] buffer)
        {
            fixed (byte* bytePtr = buffer)
            {
                fixed (char* stringPtr = s)
                {
                    return _utf8Encoding.GetBytes(
                        stringPtr, s.Length,
                        bytePtr, buffer.Length);
                }
            }
        }
    }
}
