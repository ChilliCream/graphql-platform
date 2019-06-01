using System.Collections.Generic;
using System.Xml.Schema;
using System.Buffers;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Globalization;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLReader
    {
        private static readonly byte _space = (byte)' ';
        private int _nextNewLines;
        private ReadOnlySpan<byte> _graphQLData;
        private ReadOnlySpan<byte> _value;
        private int _length;
        private int _position;

        public Utf8GraphQLReader(ReadOnlySpan<byte> graphQLData)
        {
            Kind = TokenKind.StartOfFile;
            Start = 0;
            End = 0;
            LineStart = 0;
            Line = 1;
            Column = 1;
            _graphQLData = graphQLData;
            _length = graphQLData.Length;
            _nextNewLines = 0;
            _position = 0;
            _value = null;
        }

        public ReadOnlySpan<byte> GraphQLData => _graphQLData;

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
        public int Position => _position;

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
            if (_value.Length == 0)
            {
                return string.Empty;
            }

            bool isBlockString = Kind == TokenKind.BlockString;

            int length = checked((int)_value.Length);
            bool useStackalloc =
                length <= GraphQLConstants.StackallocThreshold;

            byte[] unescapedArray = null;

            Span<byte> unescapedSpan = useStackalloc
                ? stackalloc byte[length]
                : (unescapedArray = ArrayPool<byte>.Shared.Rent(length));

            try
            {
                UnescapeValue(_value, ref unescapedSpan, isBlockString);

                fixed (byte* bytePtr = unescapedSpan)
                {
                    return StringHelper.UTF8Encoding.GetString(
                        bytePtr,
                        unescapedSpan.Length);
                }
            }
            finally
            {
                if (unescapedArray != null)
                {
                    unescapedSpan.Clear();
                    ArrayPool<byte>.Shared.Return(unescapedArray);
                }
            }
        }

        public unsafe string GetString(ReadOnlySpan<byte> unescapedValue)
        {
            if (unescapedValue.Length == 0)
            {
                return string.Empty;
            }

            fixed (byte* bytePtr = unescapedValue)
            {
                return StringHelper.UTF8Encoding
                    .GetString(bytePtr, unescapedValue.Length);
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
            if (_value.Length == 0)
            {
                unescapedValue = unescapedValue.Slice(0, 0);
            }
            else
            {
                UnescapeValue(
                    in _value,
                    ref unescapedValue,
                    Kind == TokenKind.BlockString);
            }
        }

        public bool Read()
        {
            if (_position == 0)
            {
                SkipBoml();
            }

            SkipWhitespaces();
            UpdateColumn();

            if (IsEndOfStream())
            {
                Start = _position;
                End = _position;
                Kind = TokenKind.EndOfFile;
                _value = null;
                return false;
            }

            ref readonly byte code = ref _graphQLData[_position];

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
                if (_length > _position + 2
                    && _graphQLData[_position + 1] == GraphQLConstants.Quote
                    && _graphQLData[_position + 2] == GraphQLConstants.Quote)
                {
                    _position += 2;
                    ReadBlockStringToken();
                }
                else
                {
                    ReadStringValueToken();
                }
                return true;
            }

            throw new SyntaxException(this,
                $"Unexpected character `{(char)code}` ({code}).");
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
            var start = _position;
            var position = _position;

            do
            {
                position++;
            }
            while (position < _length
                && GraphQLConstants.IsLetterOrDigitOrUnderscore(
                    in _graphQLData[position]));

            Kind = TokenKind.Name;
            Start = start;
            End = position;
            _value = _graphQLData.Slice(start, position - start);
            _position = position;
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
            Start = _position;
            End = ++_position;
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
                    if (_graphQLData[_position] == GraphQLConstants.Dot
                        && _graphQLData[_position + 1] == GraphQLConstants.Dot)
                    {
                        _position += 2;
                        End = _position;
                        Kind = TokenKind.Spread;
                    }
                    else
                    {
                        _position--;
                        throw new SyntaxException(this,
                            string.Format(CultureInfo.InvariantCulture,
                                LangResources.Reader_InvalidToken,
                                TokenKind.Spread));
                    }
                    break;

                default:
                    _position--;
                    throw new SyntaxException(this,
                        string.Format(CultureInfo.InvariantCulture,
                            LangResources.Reader_UnexpectedPunctuatorToken,
                            code));
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
            int start = _position;
            ref readonly byte code = ref firstCode;
            var isFloat = false;

            if (code == GraphQLConstants.Minus)
            {
                code = ref _graphQLData[++_position];
            }

            if (code == GraphQLConstants.Zero)
            {
                code = ref _graphQLData[++_position];
                if (GraphQLConstants.IsDigit(in code))
                {
                    throw new SyntaxException(this,
                        "Invalid number, unexpected digit after 0: " +
                        $"`{(char)code}` ({code}).");
                }
            }
            else
            {
                ReadDigits(in code);
                if (_position < _length)
                {
                    code = ref _graphQLData[_position];
                }
                else
                {
                    code = ref _space;
                }
            }

            if (code == GraphQLConstants.Dot)
            {
                isFloat = true;
                code = ref _graphQLData[++_position];
                ReadDigits(in code);
                if (_position < _length)
                {
                    code = ref _graphQLData[_position];
                }
                else
                {
                    code = ref _space;
                }
            }

            if ((code | (char)0x20) == GraphQLConstants.E)
            {
                isFloat = true;
                code = ref _graphQLData[++_position];
                if (code == GraphQLConstants.Plus
                    || code == GraphQLConstants.Minus)
                {
                    code = ref _graphQLData[++_position];
                }
                ReadDigits(in code);
            }

            Kind = isFloat ? TokenKind.Float : TokenKind.Integer;
            Start = start;
            End = _position;
            _value = _graphQLData.Slice(start, _position - start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadDigits(in byte firstCode)
        {
            if (!firstCode.IsDigit())
            {
                throw new SyntaxException(this,
                    "Invalid number, expected digit but got: " +
                    $"`{(char)firstCode}` ({firstCode}).");
            }

            while (++_position < _length
                && GraphQLConstants.IsDigit(_graphQLData[_position]))
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
            var start = _position;
            var trimStart = _position + 1;
            bool trim = true;

            while (++_position < _length
                && !GraphQLConstants.IsControlCharacter(
                    in _graphQLData[_position]))
            {
                if (trim)
                {
                    switch (_graphQLData[_position])
                    {
                        case GraphQLConstants.Hash:
                        case GraphQLConstants.Space:
                        case GraphQLConstants.Tab:
                            trimStart = _position;
                            break;

                        default:
                            trim = false;
                            break;
                    }
                }
            }

            Kind = TokenKind.Comment;
            Start = start;
            End = _position;
            _value = _graphQLData.Slice(trimStart, _position - trimStart);
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
            var start = _position;

            ref readonly byte code = ref _graphQLData[++_position];

            while (code != GraphQLConstants.NewLine
                && code != GraphQLConstants.Return)
            {
                // closing Quote (")
                if (code == GraphQLConstants.Quote)
                {
                    Kind = TokenKind.String;
                    Start = start;
                    End = _position;
                    _value = _graphQLData.Slice(start + 1, _position - start - 1);
                    _position++;
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
                    code = ref _graphQLData[++_position];
                    if (!GraphQLConstants.IsValidEscapeCharacter(in code))
                    {
                        throw new SyntaxException(this,
                            $"Invalid character escape sequence: \\{code}.");
                    }
                }

                code = ref _graphQLData[++_position];
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
            var start = _position - 2;
            _nextNewLines = 0;

            ref readonly byte code = ref _graphQLData[++_position];

            while (!IsEndOfStream())
            {
                // Closing Triple-Quote (""")
                if (code == GraphQLConstants.Quote
                    && _graphQLData[_position + 1] == GraphQLConstants.Quote
                    && _graphQLData[_position + 2] == GraphQLConstants.Quote)
                {
                    _nextNewLines--;
                    Kind = TokenKind.BlockString;
                    Start = start;
                    End = _position + 2;
                    _value = _graphQLData.Slice(start + 3, _position - start - 3);
                    _position = End + 1;
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
                    && _graphQLData[_position + 1] == GraphQLConstants.Quote
                    && _graphQLData[_position + 2] == GraphQLConstants.Quote
                    && _graphQLData[_position + 3] == GraphQLConstants.Quote)
                {
                    _position += 3;
                }
                else if (code == GraphQLConstants.NewLine)
                {
                    int next = _position + 1;
                    if (next < _length
                        && _graphQLData[next] == GraphQLConstants.Return)
                    {
                        _position = next;
                    }
                    _nextNewLines++;
                }
                else if (code == GraphQLConstants.Return)
                {
                    int next = _position + 1;
                    if (next < _length
                        && _graphQLData[next] == GraphQLConstants.NewLine)
                    {
                        _position = next;
                    }
                    _nextNewLines++;
                }

                code = ref _graphQLData[++_position];
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

            ref readonly byte code = ref _graphQLData[_position];

            while (GraphQLConstants.IsWhitespace(in code))
            {
                if (code == GraphQLConstants.NewLine)
                {
                    int next = _position + 1;
                    if (next < _length
                        && _graphQLData[next] == GraphQLConstants.Return)
                    {
                        _position = next;
                    }
                    NewLine();
                }
                else if (code == GraphQLConstants.Return)
                {
                    int next = _position + 1;
                    if (next < -_length
                        && _graphQLData[next] == GraphQLConstants.NewLine)
                    {
                        _position = next;
                    }
                    NewLine();
                }

                _position++;

                if (IsEndOfStream())
                {
                    return;
                }

                code = ref _graphQLData[_position];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SkipBoml()
        {
            ref readonly byte code = ref _graphQLData[_position];

            if (code == 239)
            {
                ref readonly byte second = ref _graphQLData[_position + 1];
                ref readonly byte third = ref _graphQLData[_position + 2];
                if (second == 187 && third == 191)
                {
                    _position += 3;
                }
            }
        }

        /// <summary>
        /// Sets the state to a new line.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NewLine()
        {
            Line++;
            LineStart = _position;
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
            LineStart = _position;
            UpdateColumn();
        }

        /// <summary>
        /// Updates the column index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateColumn()
        {
            Column = 1 + _position - LineStart;
        }

        /// <summary>
        /// Checks if the lexer source pointer has reached
        /// the end of the GraphQL source text.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEndOfStream()
        {
            return _position >= _length;
        }
    }
}
