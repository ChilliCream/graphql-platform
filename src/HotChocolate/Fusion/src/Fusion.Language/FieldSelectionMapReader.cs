using System.Diagnostics;
using System.Runtime.CompilerServices;
using static HotChocolate.Fusion.Language.Properties.FusionLanguageResources;
using TokenKind = HotChocolate.Fusion.Language.FieldSelectionMapTokenKind;

namespace HotChocolate.Fusion.Language;

/// <summary>
/// Reads syntax tokens from source text representing a field selection map.
/// </summary>
[DebuggerDisplay("{DebugState}")]
internal ref struct FieldSelectionMapReader
{
    private readonly ReadOnlySpan<char> _sourceText;
    private readonly int _length;
    private readonly int _maxAllowedTokens;

    private int _nextNewLines;
    private int _nextLineStart;
    private int _tokenCount;

    public FieldSelectionMapReader(
        ReadOnlySpan<char> sourceText,
        int maxAllowedTokens = int.MaxValue)
    {
        if (sourceText.Length == 0)
        {
            throw new ArgumentException(SourceTextCannotBeEmpty, nameof(sourceText));
        }

        _sourceText = sourceText;
        _length = sourceText.Length;
        _maxAllowedTokens = maxAllowedTokens;
    }

    /// <summary>
    /// Gets the kind of the current syntax token.
    /// </summary>
    public TokenKind TokenKind { get; private set; } = TokenKind.StartOfFile;

    /// <summary>
    /// Gets the character offset at which this syntax token begins.
    /// </summary>
    public int Start { get; private set; }

    /// <summary>
    /// Gets the character offset at which this syntax token ends.
    /// </summary>
    public int End { get; private set; }

    /// <summary>
    /// Gets the current position of the pointer within the source text.
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    /// Gets the 1-indexed line number on which the current syntax token appears.
    /// </summary>
    public int Line { get; private set; } = 1;

    /// <summary>
    /// Gets the source index at which the current line starts.
    /// </summary>
    public int LineStart { get; private set; }

    /// <summary>
    /// Gets the 1-indexed column number at which the current syntax token begins.
    /// </summary>
    public int Column { get; private set; } = 1;

    /// <summary>
    /// Gets the interpreted value of the non-punctuation token.
    /// </summary>
    public ReadOnlySpan<char> Value { get; private set; }

    /// <summary>
    /// Reads the next syntax token.
    /// </summary>
    /// <returns>
    /// Returns a boolean indicating if the read was successful.
    /// </returns>
    /// <exception cref="FieldSelectionMapSyntaxException">
    /// The source text contains an invalid syntax token.
    /// </exception>
    public bool Read()
    {
        SkipWhiteSpace();
        UpdateColumn();

        if (IsEndOfSourceText())
        {
            Start = Position;
            End = Position;
            TokenKind = TokenKind.EndOfFile;
            Value = null;

            return false;
        }

        _tokenCount++;

        if (_tokenCount > _maxAllowedTokens)
        {
            throw new FieldSelectionMapSyntaxException(
                this,
                string.Format(MaxAllowedTokensExceeded, _maxAllowedTokens));
        }

        var code = _sourceText[Position];

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

        if (code.IsQuote())
        {
            if (_length > Position + 2
                && _sourceText[Position + 1].IsQuote()
                && _sourceText[Position + 2].IsQuote())
            {
                Position += 2;
                ReadBlockStringValueToken();
            }
            else
            {
                ReadStringValueToken();
            }

            return true;
        }

        throw new FieldSelectionMapSyntaxException(this, UnexpectedCharacter, code);
    }

    public readonly TokenKind GetNextTokenKind()
    {
        if (Position >= _length)
        {
            return TokenKind.EndOfFile;
        }

        var position = Position;
        var code = _sourceText[position];

        // Skip insignificant characters.
        while (position < _length - 1)
        {
            if (code
                is CharConstants.Space
                or CharConstants.LineFeed
                or CharConstants.Return
                or CharConstants.HorizontalTab
                or CharConstants.Comma)
            {
                code = _sourceText[++position];
                continue;
            }

            break;
        }

        if (code.IsPunctuator())
        {
            return GetPunctuatorTokenKind(code);
        }

        if (code.IsLetterOrUnderscore())
        {
            return TokenKind.Name;
        }

        if (code.IsDigitOrMinus())
        {
            // The numeric lookahead is approximate. A digit or minus sign begins
            // either an integer or a float, but the kind cannot be distinguished
            // without consuming the token, so IntValue stands for any number token.
            return TokenKind.IntValue;
        }

        if (code.IsQuote())
        {
            if (_length > position + 2
                && _sourceText[position + 1].IsQuote()
                && _sourceText[position + 2].IsQuote())
            {
                return TokenKind.BlockStringValue;
            }

            return TokenKind.StringValue;
        }

        throw new FieldSelectionMapSyntaxException(this, UnexpectedCharacter, code);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Skip(TokenKind tokenKind)
    {
        if (TokenKind == tokenKind)
        {
            Read();

            return true;
        }

        return false;
    }

    private void SkipWhiteSpace()
    {
        if (_nextNewLines > 0)
        {
            NewLine(_nextNewLines, _nextLineStart);
            _nextNewLines = 0;
        }

        while (!IsEndOfSourceText())
        {
            switch (_sourceText[Position])
            {
                case CharConstants.LineFeed:
                    ++Position;
                    NewLine();
                    break;

                case CharConstants.Return:
                    if (++Position < _length
                        && _sourceText[Position] is CharConstants.LineFeed)
                    {
                        ++Position;
                    }

                    NewLine();
                    break;

                case CharConstants.Comma:
                case CharConstants.HorizontalTab:
                case CharConstants.Space:
                    ++Position;
                    break;

                default:
                    return;
            }
        }
    }

    /// <summary>
    /// Sets the state to a new line.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NewLine()
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
    /// <param name="lineStart">
    /// The source index at which the new line starts.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NewLine(int lines, int lineStart)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(lines, 1);

        Line += lines;
        LineStart = lineStart;
        UpdateColumn();
    }

    /// <summary>
    /// Reads punctuator tokens.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReadPunctuatorToken(char code)
    {
        Start = Position;
        End = ++Position;
        Value = null;
        TokenKind = GetPunctuatorTokenKind(code);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TokenKind GetPunctuatorTokenKind(char code)
    {
        return code switch
        {
            CharConstants.Colon => TokenKind.Colon,
            CharConstants.LeftAngleBracket => TokenKind.LeftAngleBracket,
            CharConstants.LeftBrace => TokenKind.LeftBrace,
            CharConstants.LeftParenthesis => TokenKind.LeftParenthesis,
            CharConstants.LeftSquareBracket => TokenKind.LeftSquareBracket,
            CharConstants.Period => TokenKind.Period,
            CharConstants.Pipe => TokenKind.Pipe,
            CharConstants.RightAngleBracket => TokenKind.RightAngleBracket,
            CharConstants.RightBrace => TokenKind.RightBrace,
            CharConstants.RightParenthesis => TokenKind.RightParenthesis,
            CharConstants.RightSquareBracket => TokenKind.RightSquareBracket,
            _ => throw new InvalidOperationException(CodeIsNotPunctuator)
        };
    }

    /// <summary>
    /// Reads name tokens.
    /// </summary>
    private void ReadNameToken()
    {
        var start = Position;
        var position = Position;

        while (++position < _length && _sourceText[position].IsLetterOrDigitOrUnderscore())
        { }

        TokenKind = TokenKind.Name;
        Start = start;
        End = position;
        Value = _sourceText[start..position];
        Position = position;
    }

    /// <summary>
    /// Reads int and float value tokens.
    /// </summary>
    private void ReadNumberToken(char firstCode)
    {
        var start = Position;
        var code = firstCode;
        var isFloat = false;

        if (code is CharConstants.Minus)
        {
            code = AdvanceAndReadOrThrowInvalidNumber();
        }

        if (code is '0' && Position + 1 < _length)
        {
            code = _sourceText[++Position];

            if (code.IsDigit())
            {
                throw new FieldSelectionMapSyntaxException(this, UnexpectedDigit, code);
            }
        }
        else
        {
            code = ReadDigits(code);
        }

        if (code is CharConstants.Period)
        {
            isFloat = true;
            code = AdvanceAndReadOrThrowInvalidNumber();
            code = ReadDigits(code);
        }

        if (code is 'e' or 'E')
        {
            isFloat = true;
            code = AdvanceAndReadOrThrowInvalidNumber();

            if (code is '+' or CharConstants.Minus)
            {
                code = AdvanceAndReadOrThrowInvalidNumber();
            }

            code = ReadDigits(code);
        }

        // A number must not be directly followed by a name start or a period.
        if (code.IsLetterOrUnderscore() || code == CharConstants.Period)
        {
            throw new FieldSelectionMapSyntaxException(this, DisallowedNameCharacterAfterNumber, code);
        }

        TokenKind = isFloat ? TokenKind.FloatValue : TokenKind.IntValue;
        Start = start;
        End = Position;
        Value = _sourceText[start..Position];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char AdvanceAndReadOrThrowInvalidNumber()
    {
        if (Position + 1 >= _length)
        {
            throw new FieldSelectionMapSyntaxException(this, InvalidNumber, CharConstants.Space);
        }

        return _sourceText[++Position];
    }

    private char ReadDigits(char firstCode)
    {
        if (!firstCode.IsDigit())
        {
            throw new FieldSelectionMapSyntaxException(this, InvalidNumber, firstCode);
        }

        char code;

        while (true)
        {
            if (++Position >= _length)
            {
                code = CharConstants.Space;
                break;
            }

            code = _sourceText[Position];

            if (!code.IsDigit())
            {
                break;
            }
        }

        return code;
    }

    /// <summary>
    /// Reads string value tokens.
    /// </summary>
    private void ReadStringValueToken()
    {
        var start = Position;

        while (++Position < _length)
        {
            var code = _sourceText[Position];

            switch (code)
            {
                case CharConstants.LineFeed:
                case CharConstants.Return:
                    throw new FieldSelectionMapSyntaxException(this, UnterminatedString);

                case CharConstants.Quote:
                    TokenKind = TokenKind.StringValue;
                    Start = start;
                    End = Position;
                    Value = _sourceText[(start + 1)..Position];
                    Position++;
                    return;

                case CharConstants.Backslash:
                    if (Position + 1 >= _length)
                    {
                        throw new FieldSelectionMapSyntaxException(this, UnterminatedString);
                    }

                    code = _sourceText[++Position];

                    if (!code.IsValidEscapeCharacter())
                    {
                        throw new FieldSelectionMapSyntaxException(
                            this,
                            InvalidCharacterEscapeSequence,
                            code);
                    }

                    break;

                default:
                    // Line feed and carriage return are handled above. Other control
                    // characters are not valid within a string value.
                    if (code.IsControlCharacter())
                    {
                        throw new FieldSelectionMapSyntaxException(
                            this,
                            InvalidCharacterWithinString,
                            code);
                    }

                    break;
            }
        }

        throw new FieldSelectionMapSyntaxException(this, UnterminatedString);
    }

    /// <summary>
    /// Reads block string value tokens.
    /// </summary>
    private void ReadBlockStringValueToken()
    {
        var start = Position - 2;
        _nextNewLines = 0;

        while (++Position < _length)
        {
            var code = _sourceText[Position];

            switch (code)
            {
                case CharConstants.LineFeed:
                    _nextNewLines++;
                    // The next line starts at the character following this line feed.
                    _nextLineStart = Position + 1;
                    break;

                case CharConstants.Return:
                    var next = Position + 1;

                    if (next < _length && _sourceText[next] is CharConstants.LineFeed)
                    {
                        Position = next;
                    }

                    _nextNewLines++;
                    // The next line starts at the character following this line terminator,
                    // which is the character after the line feed for a "\r\n" pair.
                    _nextLineStart = Position + 1;
                    break;

                // Closing triple-quote (""").
                case CharConstants.Quote:
                    if (Position + 2 < _length
                        && _sourceText[Position + 1].IsQuote()
                        && _sourceText[Position + 2].IsQuote())
                    {
                        TokenKind = TokenKind.BlockStringValue;
                        Start = start;
                        End = Position + 2;
                        Value = _sourceText[(start + 3)..Position];
                        Position = End + 1;
                        return;
                    }

                    break;

                // Escaped triple-quote (\""").
                case CharConstants.Backslash:
                    if (Position + 3 < _length
                        && _sourceText[Position + 1].IsQuote()
                        && _sourceText[Position + 2].IsQuote()
                        && _sourceText[Position + 3].IsQuote())
                    {
                        Position += 3;
                    }

                    break;

                default:
                    // Line feed and carriage return are handled above and are allowed
                    // within a block string. Other control characters are not valid.
                    if (code.IsControlCharacter())
                    {
                        throw new FieldSelectionMapSyntaxException(
                            this,
                            InvalidCharacterWithinString,
                            code);
                    }

                    break;
            }
        }

        throw new FieldSelectionMapSyntaxException(this, UnterminatedString);
    }

    /// <summary>
    /// Updates the column index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateColumn() => Column = 1 + Position - LineStart;

    /// <summary>
    /// Checks if the reader has reached the end of the source text.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsEndOfSourceText() => Position >= _length;

#if DEBUG
    private readonly string DebugState
        => _sourceText.ToString().Insert(Start, "|").Insert(End + 1, "|");
#endif
}
