using System;
using System.Runtime.CompilerServices;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLReader
    {
        private static readonly byte _space = (byte)' ';

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
            Value = null;
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
        public ReadOnlySpan<byte> Value { get; private set; }


        public bool Read()
        {
            SkipWhitespaces();
            UpdateColumn();

            if (IsEndOfStream())
            {
                Start = Position;
                End = Position;
                Kind = TokenKind.EndOfFile;
                Value = null;
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
                ReadDigits(in code);
                return true;
            }

            if (ReaderHelper.IsHash(in code))
            {
                ReadCommentToken();
            }

            if (ReaderHelper.IsQuote(in code))
            {

            }

            // TODO : fix this
            throw new SyntaxException((LexerState)null,
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
            Value = GraphQLData.Slice(start, position - start);
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
            Value = null;

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

            if (code == '0')
            {
                code = ref GraphQLData[++Position];
                if (ReaderHelper.IsDigit(in code))
                {
                    // TODO : FIX
                    throw new SyntaxException((LexerState)null,
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

            if ((code | (char)0x20) == 'e') // shift instead of or
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
            Value = GraphQLData.Slice(start, Position - start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadDigits(in byte firstCode)
        {
            if (!firstCode.IsDigit())
            {
                // TODO : FIX
                throw new SyntaxException((LexerState)null,
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
            Value = GraphQLData.Slice(trimStart, Position - trimStart);
        }

        private static bool TryReadUnicodeChar(out char code)
        {
            var c = (CharToHex(state.SourceText[++state.Position]) << 12)
                | (CharToHex(state.SourceText[++state.Position]) << 8)
                | (CharToHex(state.SourceText[++state.Position]) << 4)
                | CharToHex(state.SourceText[++state.Position]);

            if (c < 0)
            {
                code = default;
                return false;
            }

            code = (char)c;
            return true;
        }

        private static int CharToHex(int a)
        {
            return a >= 48 && a <= 57
              ? a - 48 // 0-9
              : a >= 65 && a <= 70
                ? a - 55 // A-F
                : a >= 97 && a <= 102
                  ? a - 87 // a-f
                  : -1;
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
                if (ReaderHelper.IsNewLine(in code))
                {
                    NewLine();
                }

                ++Position;
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
