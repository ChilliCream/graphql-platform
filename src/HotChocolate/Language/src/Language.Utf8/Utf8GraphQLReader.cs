using System.Runtime.CompilerServices;
using static HotChocolate.Language.Properties.LangUtf8Resources;

namespace HotChocolate.Language;

/// <summary>
/// The UTF-8 GraphQL Lexer.
/// </summary>
public ref partial struct Utf8GraphQLReader
{
    private readonly ReadOnlySpan<byte> _graphQLData;
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

    public Utf8GraphQLReader(ReadOnlySpan<byte> graphQLData, int maxAllowedTokens = int.MaxValue)
    {
        if (graphQLData.Length == 0)
        {
            throw new ArgumentException(GraphQLData_Empty, nameof(graphQLData));
        }

        _kind = TokenKind.StartOfFile;
        _start = 0;
        _end = 0;
        _lineStart = 0;
        _line = 1;
        _column = 1;
        _maxAllowedTokens = maxAllowedTokens;
        _tokenCount = 0;
        _graphQLData = graphQLData;
        _length = graphQLData.Length;
        _nextNewLines = 0;
        _position = 0;
        _value = null;
        _floatFormat = null;
    }

    /// <summary>
    /// Gets the GraphQL Data that is being read.
    /// </summary>
    public ReadOnlySpan<byte> GraphQLData => _graphQLData;

    /// <summary>
    /// Gets the kind of the current syntax token.
    /// </summary>
    public TokenKind Kind => _kind;

    /// <summary>
    /// Gets the character offset at which this node begins.
    /// </summary>
    public int Start => _start;

    /// <summary>
    /// Gets the character offset at which this node ends.
    /// </summary>
    public int End => _end;

    /// <summary>
    /// The current position of the lexer pointer.
    /// </summary>
    public int Position => _position;

    /// <summary>
    /// Gets the 1-indexed line number on which the current syntax token appears.
    /// </summary>
    public int Line => _line;

    /// <summary>
    /// The source index of where the current line starts.
    /// </summary>
    public int LineStart => _lineStart;

    /// <summary>
    /// Gets the 1-indexed column number at which the current syntax token begins.
    /// </summary>
    public int Column => _column;

    /// <summary>
    /// For non-punctuation tokens, represents the interpreted
    /// value of the token.
    /// </summary>
    public ReadOnlySpan<byte> Value => _value;

    /// <summary>
    /// Defines the type of the float if the current syntax token represents a float number.
    /// </summary>
    public FloatFormat? FloatFormat => _floatFormat;

    /// <summary>
    /// Reads the next token.
    /// </summary>
    /// <returns>
    /// Returns a boolean defining if the read was successful.
    /// </returns>
    /// <exception cref="SyntaxException">
    /// The steam contains invalid syntax tokens.
    /// </exception>
    public bool Read()
    {
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

        var code = _graphQLData[_position];

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

        if (code is GraphQLConstants.Hash)
        {
            ReadCommentToken();
            return true;
        }

        if (code is GraphQLConstants.Quote)
        {
            if (_length > _position + 2 &&
                _graphQLData[_position + 1] is GraphQLConstants.Quote &&
                _graphQLData[_position + 2] is GraphQLConstants.Quote)
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
    /// Counts the tokens and returns the document token count.
    /// </summary>
    /// <returns>
    /// Returns the token count of the document.
    /// </returns>
    public int Count()
    {
        while(Read()) { }
        return _tokenCount;
    }

    /// <summary>
    /// Reads name tokens as specified in
    /// http://facebook.github.io/graphql/October2016/#Name
    /// [_A-Za-z][_0-9A-Za-z]
    /// from the current lexer state.
    /// </summary>
    /// <returns>
    /// Returns the name token read from the current lexer state.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReadNameToken()
    {
        var start = _position;
        var position = _position;

        ReadNameToken_Next:

        if (++position < _length && _graphQLData[position].IsLetterOrDigitOrUnderscore())
        {
            goto ReadNameToken_Next;
        }

        _kind = TokenKind.Name;
        _start = start;
        _end = position;
        _value = _graphQLData.Slice(start, position - start);
        _position = position;
    }

    /// <summary>
    /// Reads punctuator tokens as specified in
    /// http://facebook.github.io/graphql/October2016/#sec-Punctuators
    /// one of ! ? $ ( ) ... . : = @ [ ] { | }
    /// additionally the reader will tokenize ampersands.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReadPunctuatorToken(byte code)
    {
        _start = _position;
        _end = ++_position;
        _value = null;

        if (code is GraphQLConstants.Dot)
        {
            if (_graphQLData[_position] is GraphQLConstants.Dot)
            {
                if (_graphQLData[_position + 1] is GraphQLConstants.Dot)
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
            else if (_graphQLData[_position].IsDigit())
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
            switch (code)
            {
                case GraphQLConstants.Bang:
                    _kind = TokenKind.Bang;
                    break;

                case GraphQLConstants.Dollar:
                    _kind = TokenKind.Dollar;
                    break;

                case GraphQLConstants.Ampersand:
                    _kind = TokenKind.Ampersand;
                    break;

                case GraphQLConstants.LeftParenthesis:
                    _kind = TokenKind.LeftParenthesis;
                    break;

                case GraphQLConstants.RightParenthesis:
                    _kind = TokenKind.RightParenthesis;
                    break;

                case GraphQLConstants.Dot:
                    _kind = TokenKind.Dot;
                    break;

                case GraphQLConstants.Colon:
                    _kind = TokenKind.Colon;
                    break;

                case GraphQLConstants.Equal:
                    _kind = TokenKind.Equal;
                    break;

                case GraphQLConstants.QuestionMark:
                    _kind = TokenKind.QuestionMark;
                    break;

                case GraphQLConstants.At:
                    _kind = TokenKind.At;
                    break;

                case GraphQLConstants.LeftBracket:
                    _kind = TokenKind.LeftBracket;
                    break;

                case GraphQLConstants.RightBracket:
                    _kind = TokenKind.RightBracket;
                    break;

                case GraphQLConstants.LeftBrace:
                    _kind = TokenKind.LeftBrace;
                    break;

                case GraphQLConstants.Pipe:
                    _kind = TokenKind.Pipe;
                    break;

                case GraphQLConstants.RightBrace:
                    _kind = TokenKind.RightBrace;
                    break;

                default:
                    // we should never get to this point since we first check
                    // if code is a punctuator.
                    throw new InvalidOperationException(
                        Utf8GraphQLReader_ReadPunctuatorToken_InvalidState);
            }
        }
    }

    /// <summary>
    /// Reads int tokens as specified in
    /// http://facebook.github.io/graphql/October2021/#IntValue
    /// or a float tokens as specified in
    /// http://facebook.github.io/graphql/October2021/#FloatValue
    /// from the current lexer state.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReadNumberToken(byte firstCode)
    {
        var start = _position;
        var code = firstCode;
        var isFloat = false;

        if (code is GraphQLConstants.Minus)
        {
            code = _graphQLData[++_position];
        }

        if (code is GraphQLConstants.Zero && !IsEndOfStream(_position + 1))
        {
            code = _graphQLData[++_position];

            if (code.IsDigit())
            {
                throw new SyntaxException(this, UnexpectedDigit, (char)code, code);
            }
        }
        else
        {
            code = ReadDigits(code);
        }

        if (code is GraphQLConstants.Dot)
        {
            isFloat = true;
            _floatFormat = Language.FloatFormat.FixedPoint;
            code = _graphQLData[++_position];
            code = ReadDigits(code);
        }

        const byte lowerCaseBit = 0x20;
        if ((code | lowerCaseBit) is GraphQLConstants.E)
        {
            isFloat = true;
            _floatFormat = Language.FloatFormat.Exponential;
            code = _graphQLData[++_position];

            if (code is GraphQLConstants.Plus or GraphQLConstants.Minus)
            {
                code = _graphQLData[++_position];
            }
            code = ReadDigits(code);
        }

        // Lookahead for NameStart.
        // https://github.com/graphql/graphql-spec/pull/601
        // NOTE:
        // Not checking for Digit because there is no situation
        // where that hasn't been consumed at this point.
        if (code.IsLetterOrUnderscore() ||
            code == GraphQLConstants.Dot)
        {
            throw new SyntaxException(this, DisallowedNameCharacterAfterNumber, (char)code, code);
        }

        _kind = isFloat
            ? TokenKind.Float
            : TokenKind.Integer;
        _start = start;
        _end = _position;
        _value = _graphQLData.Slice(start, _position - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                code = GraphQLConstants.Space;
                break;
            }

            code = _graphQLData[_position];

            if (!code.IsDigit())
            {
                break;
            }
        }

        return code;
    }

    /// <summary>
    /// Reads comment tokens as specified in
    /// http://facebook.github.io/graphql/October2016/#sec-Comments
    /// #[\u0009\u0020-\uFFFF]*
    /// from the current lexer state.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReadCommentToken()
    {
        var start = _position;
        var trimStart = _position + 1;
        var trim = true;
        var run = true;

        while (run && ++_position < _length)
        {
            var code = _graphQLData[_position];

            switch (code)
            {
                // SourceCharacter
                case GraphQLConstants.Null:
                case GraphQLConstants.StartOfHeading:
                case GraphQLConstants.StartOfText:
                case GraphQLConstants.EndOfText:
                case GraphQLConstants.EndOfTransmission:
                case GraphQLConstants.Enquiry:
                case GraphQLConstants.Acknowledgement:
                case GraphQLConstants.Bell:
                case GraphQLConstants.Backspace:
                case GraphQLConstants.LineFeed:
                case GraphQLConstants.VerticalTab:
                case GraphQLConstants.FormFeed:
                case GraphQLConstants.Return:
                case GraphQLConstants.ShiftOut:
                case GraphQLConstants.ShiftIn:
                case GraphQLConstants.DataLinkEscape:
                case GraphQLConstants.DeviceControl1:
                case GraphQLConstants.DeviceControl2:
                case GraphQLConstants.DeviceControl3:
                case GraphQLConstants.DeviceControl4:
                case GraphQLConstants.NegativeAcknowledgement:
                case GraphQLConstants.SynchronousIdle:
                case GraphQLConstants.EndOfTransmissionBlock:
                case GraphQLConstants.Cancel:
                case GraphQLConstants.EndOfMedium:
                case GraphQLConstants.Substitute:
                case GraphQLConstants.Escape:
                case GraphQLConstants.FileSeparator:
                case GraphQLConstants.GroupSeparator:
                case GraphQLConstants.RecordSeparator:
                case GraphQLConstants.UnitSeparator:
                case GraphQLConstants.Delete:
                    run = false;
                    break;

                case GraphQLConstants.Hash:
                case GraphQLConstants.Space:
                case GraphQLConstants.HorizontalTab:
                    if (trim)
                    {
                        trimStart = _position;
                    }
                    break;

                default:
                    trim = false;
                    break;
            }
        }

        _kind = TokenKind.Comment;
        _start = start;
        _end = _position;
        _value = _graphQLData.Slice(trimStart, _position - trimStart);
    }

    /// <summary>
    /// Reads string tokens as specified in
    /// http://facebook.github.io/graphql/October2016/#StringValue
    /// "([^"\\\u000A\u000D]|(\\(u[0-9a-fA-F]{4}|["\\/bfnrt])))*"
    /// from the current lexer state.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReadStringValueToken()
    {
        var start = _position;

        while (++_position < _length)
        {
            var code = _graphQLData[_position];

            switch (code)
            {
                case GraphQLConstants.LineFeed:
                case GraphQLConstants.Return:
                    return;

                // closing Quote (")
                case GraphQLConstants.Quote:
                    _kind = TokenKind.String;
                    _start = start;
                    _end = _position;
                    _value = _graphQLData.Slice(
                        start + 1,
                        _position - start - 1);
                    _position++;
                    return;

                case GraphQLConstants.Backslash:
                    code = _graphQLData[++_position];

                    if (!code.IsValidEscapeCharacter())
                    {
                        throw new SyntaxException(this, InvalidCharacterEscapeSequence, code);
                    }
                    break;

                // SourceCharacter
                case GraphQLConstants.Null:
                case GraphQLConstants.StartOfHeading:
                case GraphQLConstants.StartOfText:
                case GraphQLConstants.EndOfText:
                case GraphQLConstants.EndOfTransmission:
                case GraphQLConstants.Enquiry:
                case GraphQLConstants.Acknowledgement:
                case GraphQLConstants.Bell:
                case GraphQLConstants.Backspace:
                case GraphQLConstants.VerticalTab:
                case GraphQLConstants.FormFeed:
                case GraphQLConstants.ShiftOut:
                case GraphQLConstants.ShiftIn:
                case GraphQLConstants.DataLinkEscape:
                case GraphQLConstants.DeviceControl1:
                case GraphQLConstants.DeviceControl2:
                case GraphQLConstants.DeviceControl3:
                case GraphQLConstants.DeviceControl4:
                case GraphQLConstants.NegativeAcknowledgement:
                case GraphQLConstants.SynchronousIdle:
                case GraphQLConstants.EndOfTransmissionBlock:
                case GraphQLConstants.Cancel:
                case GraphQLConstants.EndOfMedium:
                case GraphQLConstants.Substitute:
                case GraphQLConstants.Escape:
                case GraphQLConstants.FileSeparator:
                case GraphQLConstants.GroupSeparator:
                case GraphQLConstants.RecordSeparator:
                case GraphQLConstants.UnitSeparator:
                case GraphQLConstants.Delete:
                    throw new SyntaxException(this, InvalidCharacterWithinString, code);
            }
        }

        throw new SyntaxException(this, UnterminatedString);
    }

    /// <summary>
    /// Reads block string tokens as specified in
    /// http://facebook.github.io/graphql/draft/#BlockStringCharacter
    /// from the current lexer state.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReadBlockStringToken()
    {
        var start = _position - 2;
        _nextNewLines = 0;

        while (++_position < _length)
        {
            var code = _graphQLData[_position];

            switch (code)
            {
                case GraphQLConstants.LineFeed:
                    _nextNewLines++;
                    break;

                case GraphQLConstants.Return:
                    var next = _position + 1;

                    if (next < _length && _graphQLData[next] is GraphQLConstants.LineFeed)
                    {
                        _position = next;
                    }
                    _nextNewLines++;
                    break;

                // Closing Triple-Quote (""")
                case GraphQLConstants.Quote:
                    if (_graphQLData[_position + 1] is GraphQLConstants.Quote &&
                        _graphQLData[_position + 2] is GraphQLConstants.Quote)
                    {
                        _kind = TokenKind.BlockString;
                        _start = start;
                        _end = _position + 2;
                        _value = _graphQLData.Slice(
                            start + 3,
                            _position - start - 3);
                        _position = _end + 1;
                        return;
                    }
                    break;

                case GraphQLConstants.Backslash:
                    if (_graphQLData[_position + 1] is GraphQLConstants.Quote &&
                        _graphQLData[_position + 2] is GraphQLConstants.Quote &&
                        _graphQLData[_position + 3] is GraphQLConstants.Quote)
                    {
                        _position += 3;
                    }
                    break;

                // SourceCharacter
                case GraphQLConstants.Null:
                case GraphQLConstants.StartOfHeading:
                case GraphQLConstants.StartOfText:
                case GraphQLConstants.EndOfText:
                case GraphQLConstants.EndOfTransmission:
                case GraphQLConstants.Enquiry:
                case GraphQLConstants.Acknowledgement:
                case GraphQLConstants.Bell:
                case GraphQLConstants.Backspace:
                case GraphQLConstants.VerticalTab:
                case GraphQLConstants.FormFeed:
                case GraphQLConstants.ShiftOut:
                case GraphQLConstants.ShiftIn:
                case GraphQLConstants.DataLinkEscape:
                case GraphQLConstants.DeviceControl1:
                case GraphQLConstants.DeviceControl2:
                case GraphQLConstants.DeviceControl3:
                case GraphQLConstants.DeviceControl4:
                case GraphQLConstants.NegativeAcknowledgement:
                case GraphQLConstants.SynchronousIdle:
                case GraphQLConstants.EndOfTransmissionBlock:
                case GraphQLConstants.Cancel:
                case GraphQLConstants.EndOfMedium:
                case GraphQLConstants.Substitute:
                case GraphQLConstants.Escape:
                case GraphQLConstants.FileSeparator:
                case GraphQLConstants.GroupSeparator:
                case GraphQLConstants.RecordSeparator:
                case GraphQLConstants.UnitSeparator:
                case GraphQLConstants.Delete:
                    throw new SyntaxException(
                        this,
                        string.Format(InvalidCharacterWithinString, code));
            }
        }

        throw new SyntaxException(this, UnterminatedString);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SkipWhitespaces()
    {
        if (_nextNewLines > 0)
        {
            NewLine(_nextNewLines);
            _nextNewLines = 0;
        }

        while (!IsEndOfStream())
        {
            var code = _graphQLData[_position];

            switch (code)
            {
                case GraphQLConstants.LineFeed:
                    ++_position;
                    NewLine();
                    break;

                case GraphQLConstants.Return:
                    if (++_position < _length &&
                        _graphQLData[_position] is GraphQLConstants.LineFeed)
                    {
                        ++_position;
                    }
                    NewLine();
                    break;

                case GraphQLConstants.HorizontalTab:
                case GraphQLConstants.Space:
                case GraphQLConstants.Comma:
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
        var code = _graphQLData[_position];

        if (code is 239)
        {
            if (_graphQLData[_position + 1] is 187 && _graphQLData[_position + 2] is 191)
            {
                _position += 3;
            }
        }

        if (code is 254)
        {
            if (_graphQLData[_position + 1] is 255)
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
