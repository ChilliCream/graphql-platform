using System.Buffers;
using System.Runtime.CompilerServices;
using static HotChocolate.Language.Properties.LangUtf8Resources;

namespace HotChocolate.Language;

/// <summary>
/// A low-level, high-performance lexer (tokenizer) that reads UTF-8 encoded GraphQL source text.
/// </summary>
public ref partial struct Utf8GraphQLReader
{
    private readonly ReadOnlySpan<byte> _sourceText;
    private readonly ReadOnlySequence<byte> _sequence;
    private ReadOnlySpan<byte> _currentSpan;
    private int _currentSpanIndex;
    private int _segmentOffset;
    private byte[]? _rentedBuffer;
    private SequencePosition _nextSegmentPosition;
    private readonly bool _isMultiSegment;
    private readonly int _length;
    private readonly int _maxAllowedTokens;
    private int _nextNewLines;
    private ReadOnlySpan<byte> _value;
    private FloatFormat? _floatFormat;
    private int _position;
    private TokenKind _kind;
    private int _start;
    private int _end;
    private int _line;
    private int _lineStart;
    private int _column;
    private int _tokenCount;

    /// <summary>
    /// Initializes a new instance of <see cref="Utf8GraphQLReader"/> for reading
    /// a contiguous UTF-8 encoded GraphQL source text.
    /// </summary>
    /// <param name="sourceText">
    /// The UTF-8 encoded GraphQL source text to read.
    /// </param>
    /// <param name="maxAllowedTokens">
    /// The maximum number of tokens the reader is allowed to read before throwing a
    /// <see cref="SyntaxException"/>. Defaults to <see cref="int.MaxValue"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="sourceText"/> is empty.
    /// </exception>
    public Utf8GraphQLReader(ReadOnlySpan<byte> sourceText, int maxAllowedTokens = int.MaxValue)
    {
        if (sourceText.Length == 0)
        {
            throw new ArgumentException(GraphQLData_Empty, nameof(sourceText));
        }

        _kind = TokenKind.StartOfFile;
        _start = 0;
        _end = 0;
        _lineStart = 0;
        _line = 1;
        _column = 1;
        _maxAllowedTokens = maxAllowedTokens;
        _tokenCount = 0;
        _sourceText = sourceText;
        _length = sourceText.Length;
        _nextNewLines = 0;
        _position = 0;
        _value = null;
        _floatFormat = null;
    }

    /// <summary>
    /// Gets the UTF-8 encoded GraphQL source text that is being read.
    /// </summary>
    public ReadOnlySpan<byte> SourceText => _sourceText;

    /// <summary>
    /// Gets the kind of the current syntax token.
    /// </summary>
    public TokenKind Kind => _kind;

    /// <summary>
    /// Gets the character offset at which the current token begins.
    /// </summary>
    public int Start => _start;

    /// <summary>
    /// Gets the character offset at which the current token ends.
    /// </summary>
    public int End => _end;

    /// <summary>
    /// Gets the current position of the lexer pointer in the source text.
    /// </summary>
    public int Position => _position;

    /// <summary>
    /// Gets the 1-indexed line number on which the current syntax token appears.
    /// </summary>
    public int Line => _line;

    /// <summary>
    /// Gets the source index of where the current line starts.
    /// </summary>
    public int LineStart => _lineStart;

    /// <summary>
    /// Gets the 1-indexed column number at which the current syntax token begins.
    /// </summary>
    public int Column => _column;

    /// <summary>
    /// Gets the interpreted value of the current token as a UTF-8 byte span.
    /// For punctuation tokens, the value is empty.
    /// </summary>
    public ReadOnlySpan<byte> Value => _value;

    /// <summary>
    /// Gets the float format of the current token, or <c>null</c> if the token is not a float.
    /// </summary>
    public FloatFormat? FloatFormat => _floatFormat;

    /// <summary>
    /// Reads the next token from the source text.
    /// </summary>
    /// <returns>
    /// <c>true</c> if a token was successfully read;
    /// <c>false</c> if the end of the source text has been reached.
    /// </returns>
    /// <exception cref="SyntaxException">
    /// The source text contains invalid syntax.
    /// </exception>
    public bool Read()
    {
        if (_isMultiSegment)
        {
            return ReadMultiSegment();
        }

        _floatFormat = null;

        if (_position == 0)
        {
            SkipBom();
        }

        SkipWhitespaces();
        UpdateColumn();

        if (IsEndOfStream())
        {
            _start = _position;
            _end = _position;
            _kind = TokenKind.EndOfFile;
            _value = null;
            return false;
        }

        _tokenCount++;

        if (_tokenCount > _maxAllowedTokens)
        {
            throw new SyntaxException(
                this,
                string.Format(
                    Utf8GraphQLReader_Read_MaxAllowedTokensReached,
                    _maxAllowedTokens));
        }

        var code = _sourceText[_position];

        if (code.IsPunctuator())
        {
            ReadPunctuatorToken(code);
            return true;
        }

        if (code.IsLetterOrUnderscore())
        {
            ReadNameToken();
            return true;
        }

        if (code.IsDigitOrMinus())
        {
            ReadNumberToken(code);
            return true;
        }

        if (code is GraphQLCharacters.Hash)
        {
            ReadCommentToken();
            return true;
        }

        if (code is GraphQLCharacters.Quote)
        {
            if (_length > _position + 2
                && _sourceText[_position + 1] is GraphQLCharacters.Quote
                && _sourceText[_position + 2] is GraphQLCharacters.Quote)
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

        throw new SyntaxException(this, UnexpectedCharacter, (char)code, code);
    }

    /// <summary>
    /// Reads all remaining tokens and returns the total token count.
    /// </summary>
    /// <returns>
    /// The total number of tokens in the source text.
    /// </returns>
    public int Count()
    {
        while (Read())
        { }
        return _tokenCount;
    }

    /// <summary>
    /// Reads name tokens as specified in
    /// https://spec.graphql.org/September2025/#Name
    /// [_A-Za-z][_0-9A-Za-z]
    /// from the current lexer state.
    /// </summary>
    /// <returns>
    /// Returns the name token read from the current lexer state.
    /// </returns>
    private void ReadNameToken()
    {
        var start = _position;
        var position = _position;

ReadNameToken_Next:
        if (++position < _length && _sourceText[position].IsLetterOrDigitOrUnderscore())
        {
            goto ReadNameToken_Next;
        }

        _kind = TokenKind.Name;
        _start = start;
        _end = position;
        _value = _sourceText.Slice(start, position - start);
        _position = position;
    }

    /// <summary>
    /// Reads punctuator tokens as specified in
    /// https://spec.graphql.org/September2025/#sec-Punctuators
    /// one of ! ? $ ( ) ... . : = @ [ ] { | }
    /// additionally the reader will tokenize ampersands.
    /// </summary>
    private void ReadPunctuatorToken(byte code)
    {
        _start = _position;
        _end = ++_position;
        _value = null;

        if (code is GraphQLCharacters.Dot)
        {
            if (IsEndOfStream())
            {
                _kind = TokenKind.Dot;
            }
            else if (_sourceText[_position] is GraphQLCharacters.Dot)
            {
                if (!IsEndOfStream(_position + 1)
                    && _sourceText[_position + 1] is GraphQLCharacters.Dot)
                {
                    _position += 2;
                    _end = _position;
                    _kind = TokenKind.Spread;
                }
                else
                {
                    _position--;
                    throw ThrowHelper.Reader_InvalidToken(this, TokenKind.Spread);
                }
            }
            else if (_sourceText[_position].IsDigit())
            {
                _position--;
                throw ThrowHelper.Reader_UnexpectedDigitAfterDot(this);
            }
            else
            {
                _kind = TokenKind.Dot;
            }
        }
        else
        {
            _kind = GraphQLCharacters.PunctuatorKind[code];
        }
    }

    /// <summary>
    /// Reads int tokens as specified in
    /// https://spec.graphql.org/September2025/#IntValue
    /// or a float tokens as specified in
    /// https://spec.graphql.org/September2025/#FloatValue
    /// from the current lexer state.
    /// </summary>
    private void ReadNumberToken(byte firstCode)
    {
        var start = _position;
        var code = firstCode;
        var isFloat = false;

        if (code is GraphQLCharacters.Minus)
        {
            code = AdvanceAndReadOrThrowInvalidNumber();
        }

        if (code is GraphQLCharacters.Zero && !IsEndOfStream(_position + 1))
        {
            code = _sourceText[++_position];

            if (code.IsDigit())
            {
                throw new SyntaxException(this, UnexpectedDigit, (char)code, code);
            }
        }
        else
        {
            code = ReadDigits(code);
        }

        if (code is GraphQLCharacters.Dot)
        {
            isFloat = true;
            _floatFormat = Language.FloatFormat.FixedPoint;
            code = AdvanceAndReadOrThrowInvalidNumber();
            code = ReadDigits(code);
        }

        const byte lowerCaseBit = 0x20;
        if ((code | lowerCaseBit) is GraphQLCharacters.E)
        {
            isFloat = true;
            _floatFormat = Language.FloatFormat.Exponential;
            code = AdvanceAndReadOrThrowInvalidNumber();

            if (code is GraphQLCharacters.Plus or GraphQLCharacters.Minus)
            {
                code = AdvanceAndReadOrThrowInvalidNumber();
            }
            code = ReadDigits(code);
        }

        // Lookahead for NameStart.
        // https://github.com/graphql/graphql-spec/pull/601
        // NOTE:
        // Not checking for Digit because there is no situation
        // where that hasn't been consumed at this point.
        if (code.IsLetterOrUnderscore()
            || code == GraphQLCharacters.Dot)
        {
            throw new SyntaxException(this, DisallowedNameCharacterAfterNumber, (char)code, code);
        }

        _kind = isFloat
            ? TokenKind.Float
            : TokenKind.Integer;
        _start = start;
        _end = _position;
        _value = _sourceText.Slice(start, _position - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte AdvanceAndReadOrThrowInvalidNumber()
    {
        if (IsEndOfStream(_position + 1))
        {
            throw new SyntaxException(
                this,
                InvalidNumber,
                (char)GraphQLCharacters.Space,
                GraphQLCharacters.Space);
        }

        return _sourceText[++_position];
    }

    private byte ReadDigits(byte firstCode)
    {
        if (!firstCode.IsDigit())
        {
            throw new SyntaxException(this, InvalidNumber, (char)firstCode, firstCode);
        }

        byte code;

        while (true)
        {
            if (++_position >= _length)
            {
                code = GraphQLCharacters.Space;
                break;
            }

            code = _sourceText[_position];

            if (!code.IsDigit())
            {
                break;
            }
        }

        return code;
    }

    /// <summary>
    /// Reads comment tokens as specified in
    /// https://spec.graphql.org/September2025/#sec-Comments
    /// #[\u0009\u0020-\uFFFF]*
    /// from the current lexer state.
    /// </summary>
    private void ReadCommentToken()
    {
        var start = _position;
        var trimStart = _position + 1;
        var trim = true;

        while (++_position < _length)
        {
            var code = _sourceText[_position];

            // Control characters (0x00-0x1F except Tab) and DEL terminate the comment.
            if (code.IsControlCharacter())
            {
                break;
            }

            if (code is GraphQLCharacters.Hash
                or GraphQLCharacters.Space
                or GraphQLCharacters.HorizontalTab)
            {
                if (trim)
                {
                    trimStart = _position;
                }
            }
            else
            {
                trim = false;
            }
        }

        _kind = TokenKind.Comment;
        _start = start;
        _end = _position;
        _value = _sourceText.Slice(trimStart, _position - trimStart);
    }

    /// <summary>
    /// Reads string tokens as specified in
    /// https://spec.graphql.org/September2025/#StringValue
    /// "([^"\\\u000A\u000D]|(\\(u[0-9a-fA-F]{4}|["\\/bfnrt])))*"
    /// from the current lexer state.
    /// </summary>
    private void ReadStringValueToken()
    {
        var start = _position;

        while (++_position < _length)
        {
            var code = _sourceText[_position];

            switch (code)
            {
                case GraphQLCharacters.LineFeed:
                case GraphQLCharacters.Return:
                    throw new SyntaxException(this, UnterminatedString);

                // closing Quote (")
                case GraphQLCharacters.Quote:
                    _kind = TokenKind.String;
                    _start = start;
                    _end = _position;
                    _value = _sourceText.Slice(
                        start + 1,
                        _position - start - 1);
                    _position++;
                    return;

                case GraphQLCharacters.Backslash:
                    if (IsEndOfStream(_position + 1))
                    {
                        throw new SyntaxException(this, UnterminatedString);
                    }

                    code = _sourceText[++_position];

                    if (!code.IsValidEscapeCharacter())
                    {
                        throw new SyntaxException(this, InvalidCharacterEscapeSequence, code);
                    }
                    break;

                default:
                    // Control characters (0x00-0x1F except Tab) and DEL are not
                    // valid within strings. LF and CR are handled above.
                    if (code.IsControlCharacter())
                    {
                        throw new SyntaxException(this, InvalidCharacterWithinString, code);
                    }
                    break;
            }
        }

        throw new SyntaxException(this, UnterminatedString);
    }

    /// <summary>
    /// Reads block string tokens as specified in
    /// https://spec.graphql.org/September2025/#BlockStringCharacter
    /// from the current lexer state.
    /// </summary>
    private void ReadBlockStringToken()
    {
        var start = _position - 2;
        _nextNewLines = 0;

        while (++_position < _length)
        {
            var code = _sourceText[_position];

            switch (code)
            {
                case GraphQLCharacters.LineFeed:
                    _nextNewLines++;
                    break;

                case GraphQLCharacters.Return:
                    var next = _position + 1;

                    if (next < _length && _sourceText[next] is GraphQLCharacters.LineFeed)
                    {
                        _position = next;
                    }
                    _nextNewLines++;
                    break;

                // Closing Triple-Quote (""")
                case GraphQLCharacters.Quote:
                    if (!IsEndOfStream(_position + 2)
                        && _sourceText[_position + 1] is GraphQLCharacters.Quote
                        && _sourceText[_position + 2] is GraphQLCharacters.Quote)
                    {
                        _kind = TokenKind.BlockString;
                        _start = start;
                        _end = _position + 2;
                        _value = _sourceText.Slice(
                            start + 3,
                            _position - start - 3);
                        _position = _end + 1;
                        return;
                    }
                    break;

                case GraphQLCharacters.Backslash:
                    if (!IsEndOfStream(_position + 3)
                        && _sourceText[_position + 1] is GraphQLCharacters.Quote
                        && _sourceText[_position + 2] is GraphQLCharacters.Quote
                        && _sourceText[_position + 3] is GraphQLCharacters.Quote)
                    {
                        _position += 3;
                    }
                    break;

                default:
                    // Control characters (0x00-0x1F except Tab) and DEL are not
                    // valid within block strings. LF and CR are handled above.
                    if (code.IsControlCharacter())
                    {
                        throw new SyntaxException(
                            this,
                            string.Format(InvalidCharacterWithinString, code));
                    }
                    break;
            }
        }

        throw new SyntaxException(this, UnterminatedString);
    }

    private void SkipWhitespaces()
    {
        if (_nextNewLines > 0)
        {
            NewLine(_nextNewLines);
            _nextNewLines = 0;
        }

        while (!IsEndOfStream())
        {
            var code = _sourceText[_position];

            switch (code)
            {
                case GraphQLCharacters.LineFeed:
                    ++_position;
                    NewLine();
                    break;

                case GraphQLCharacters.Return:
                    if (++_position < _length
                        && _sourceText[_position] is GraphQLCharacters.LineFeed)
                    {
                        ++_position;
                    }
                    NewLine();
                    break;

                case GraphQLCharacters.HorizontalTab:
                case GraphQLCharacters.Space:
                case GraphQLCharacters.Comma:
                    ++_position;
                    break;

                default:
                    return;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SkipBom()
    {
        var code = _sourceText[_position];

        if (code is 239)
        {
            if (!IsEndOfStream(_position + 2)
                && _sourceText[_position + 1] is 187
                && _sourceText[_position + 2] is 191)
            {
                _position += 3;
            }
        }

        if (code is 254)
        {
            if (!IsEndOfStream(_position + 1)
                && _sourceText[_position + 1] is 255)
            {
                _position += 2;
            }
        }
    }

    /// <summary>
    /// Sets the state to a new line.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NewLine()
    {
        _line++;
        _lineStart = _position;
        UpdateColumn();
    }

    /// <summary>
    /// Sets the state to a new line.
    /// </summary>
    /// <param name="lines">
    /// The number of lines to skip.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NewLine(int lines)
    {
        if (lines < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lines),
                lines,
                NewLineMustBeGreaterOrEqualToOne);
        }

        _line += lines;
        _lineStart = _position;
        UpdateColumn();
    }

    /// <summary>
    /// Updates the column index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateColumn() => _column = 1 + _position - _lineStart;

    /// <summary>
    /// Checks if the lexer source pointer has reached
    /// the end of the GraphQL source text.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsEndOfStream() => _position >= _length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsEndOfStream(int position) => position >= _length;
}
