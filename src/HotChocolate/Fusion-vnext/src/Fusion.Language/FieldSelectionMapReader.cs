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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SkipWhiteSpace()
    {
        if (_nextNewLines > 0)
        {
            NewLine(_nextNewLines);
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NewLine(int lines)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(lines, 1);

        Line += lines;
        LineStart = Position;
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
            CharConstants.LeftSquareBracket => TokenKind.LeftSquareBracket,
            CharConstants.Period => TokenKind.Period,
            CharConstants.Pipe => TokenKind.Pipe,
            CharConstants.RightAngleBracket => TokenKind.RightAngleBracket,
            CharConstants.RightBrace => TokenKind.RightBrace,
            CharConstants.RightSquareBracket => TokenKind.RightSquareBracket,
            _ => throw new InvalidOperationException(CodeIsNotPunctuator)
        };
    }

    /// <summary>
    /// Reads name tokens.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
