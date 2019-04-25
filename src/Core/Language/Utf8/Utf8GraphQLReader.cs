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
            byte[] unescaped = ArrayPool<byte>.Shared.Rent(_value.Length);

            try
            {
                var unescapedSpan = new Span<byte>(unescaped);
                UnescapeValue(ref unescapedSpan);

                fixed (byte* bytePtr = unescaped)
                {
                    return _utf8Encoding.GetString(bytePtr, _value.Length);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(unescaped);
            }
        }

        public unsafe string GetString(ReadOnlySpan<byte> unescapedValue)
        {
            fixed (byte* bytePtr = unescapedValue)
            {
                return _utf8Encoding.GetString(bytePtr, _value.Length);
            }
        }

        public void UnescapeValue(ref Span<byte> unescapedValue)
        {
            bool isBlockString = Kind == TokenKind.BlockString;

            Utf8Helper.Unescape(
                in _value,
                ref unescapedValue,
                isBlockString);

            if (isBlockString)
            {
                BlockStringHelper.TrimBlockStringToken(
                    unescapedValue, ref unescapedValue);
            }
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

            if (ReaderHelper.IsLetterOrDigitOrUnderscore(in code))
            {
                ReadNameToken();
                return true;
            }

            if (ReaderHelper.IsPunctuator(in code))
            {
                ReadPunctuatorToken(in code);
                return true;
            }

            if (ReaderHelper.IsDigitOrMinus(in code))
            {
                ReadNumberToken(in code);
                return true;
            }

            if (ReaderHelper.IsHash(in code))
            {
                ReadCommentToken();
                return true;
            }

            if (ReaderHelper.IsQuote(in code))
            {
                if (ReaderHelper.IsQuote(in GraphQLData[Position + 1])
                    && ReaderHelper.IsQuote(in GraphQLData[Position + 2]))
                {
                    Position += 2;
                    ReadBlockStringToken();
                }
                ReadStringValueToken();
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
                && ReaderHelper.IsLetterOrDigitOrUnderscore(
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
                case ReaderHelper.Bang:
                    Kind = TokenKind.Bang;
                    break;

                case ReaderHelper.Dollar:
                    Kind = TokenKind.Dollar;
                    break;

                case ReaderHelper.Ampersand:
                    Kind = TokenKind.Ampersand;
                    break;

                case ReaderHelper.LeftParenthesis:
                    Kind = TokenKind.LeftParenthesis;
                    break;

                case ReaderHelper.RightParenthesis:
                    Kind = TokenKind.RightParenthesis;
                    break;

                case ReaderHelper.Colon:
                    Kind = TokenKind.Colon;
                    break;

                case ReaderHelper.Equal:
                    Kind = TokenKind.Equal;
                    break;

                case ReaderHelper.At:
                    Kind = TokenKind.At;
                    break;

                case ReaderHelper.LeftBracket:
                    Kind = TokenKind.LeftBracket;
                    break;

                case ReaderHelper.RightBracket:
                    Kind = TokenKind.RightBracket;
                    break;

                case ReaderHelper.LeftBrace:
                    Kind = TokenKind.LeftBrace;
                    break;

                case ReaderHelper.RightBrace:
                    Kind = TokenKind.RightBrace;
                    break;

                case ReaderHelper.Pipe:
                    Kind = TokenKind.Pipe;
                    break;

                case ReaderHelper.Dot:
                    if (ReaderHelper.IsDot(in GraphQLData[Position])
                        && ReaderHelper.IsDot(in GraphQLData[Position + 1]))
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

            if (ReaderHelper.IsMinus(in code))
            {
                code = ref GraphQLData[++Position];
            }

            if (code == ReaderHelper.Zero)
            {
                code = ref GraphQLData[++Position];
                if (ReaderHelper.IsDigit(in code))
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

            if (code.IsDot())
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

            if ((code | (char)0x20) == ReaderHelper.E)
            {
                isFloat = true;
                code = ref GraphQLData[++Position];
                if (ReaderHelper.IsPlus(in code)
                    || ReaderHelper.IsMinus(in code))
                {
                    code = ref GraphQLData[++Position];
                }
                ReadDigits(in code);
            }

            Kind = isFloat ? TokenKind.Float : TokenKind.Integer;
            Start = start;
            End = Position;
            _value = GraphQLData.Slice(start, Position - start);
            Position++;
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
                && ReaderHelper.IsDigit(GraphQLData[Position]))
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
                && !ReaderHelper.IsControlCharacter(in GraphQLData[Position]))
            {
                if (trim)
                {
                    switch (GraphQLData[Position])
                    {
                        case ReaderHelper.Hash:
                        case ReaderHelper.Space:
                        case ReaderHelper.Tab:
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

            while (!ReaderHelper.IsNewLine(in code))
            {
                // closing Quote (")
                if (ReaderHelper.IsQuote(in code))
                {
                    Kind = TokenKind.String;
                    Start = start;
                    End = Position;
                    _value = GraphQLData.Slice(start, Position - start);
                    Position++;
                    return;
                }

                // SourceCharacter
                if (ReaderHelper.IsControlCharacter(in code))
                {
                    throw new SyntaxException(this,
                        $"Invalid character within String: {code}.");
                }

                if (ReaderHelper.IsBackslash(in code)
                    && ReaderHelper.IsQuote(in GraphQLData[Position + 1]))
                {
                    Position++;
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
            var start = Position;

            ref readonly byte code = ref GraphQLData[++Position];

            while (!IsEndOfStream())
            {
                // Closing Triple-Quote (""")
                if (code.IsQuote()
                    && ReaderHelper.IsQuote(in GraphQLData[Position + 1])
                    && ReaderHelper.IsQuote(in GraphQLData[Position + 2]))
                {
                    var length = Position - start - 2;

                    Kind = TokenKind.String;
                    Start = start;
                    End = Position;
                    _value = GraphQLData.Slice(start, length);

                    int newLines = BlockStringHelper.CountLines(in _value) - 1;
                    if (newLines > 0)
                    {
                        NewLine(newLines);
                    }

                    Position++;
                    return;
                }

                // SourceCharacter
                if (code.IsControlCharacter()
                    && !code.IsNewLine()
                    && !code.IsReturn())
                {
                    throw new SyntaxException(this,
                        $"Invalid character within String: ${code}.");
                }

                if (ReaderHelper.IsBackslash(in code)
                    && ReaderHelper.IsQuote(in GraphQLData[Position + 1])
                    && ReaderHelper.IsQuote(in GraphQLData[Position + 2])
                    && ReaderHelper.IsQuote(in GraphQLData[Position + 3]))
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

            ref readonly byte code = ref GraphQLData[Position];

            while (ReaderHelper.IsWhitespace(in code))
            {
                if (code == ReaderHelper.NewLine)
                {
                    Position++;
                    if (!IsEndOfStream()
                        && GraphQLData[Position + 1] == ReaderHelper.Return)
                    {
                        Position++;
                    }
                    NewLine();
                }
                else if (code == ReaderHelper.Return)
                {
                    Position++;
                    if (!IsEndOfStream()
                        && GraphQLData[Position + 1] == ReaderHelper.NewLine)
                    {
                        Position++;
                    }
                    NewLine();
                }
                else
                {
                    ++Position;
                }

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
    }
}
