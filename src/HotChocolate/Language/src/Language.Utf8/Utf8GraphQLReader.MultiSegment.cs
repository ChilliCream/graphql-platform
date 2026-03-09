using System.Buffers;
using System.Runtime.CompilerServices;
using static HotChocolate.Language.Properties.LangUtf8Resources;

namespace HotChocolate.Language;

public ref partial struct Utf8GraphQLReader
{
    /// <summary>
    /// Initializes a new instance of <see cref="Utf8GraphQLReader"/> for reading
    /// a possibly multi-segment UTF-8 encoded GraphQL source text.
    /// </summary>
    /// <param name="sourceText">
    /// The UTF-8 encoded GraphQL source text to read, provided as a
    /// <see cref="ReadOnlySequence{T}"/> that may span multiple memory segments.
    /// </param>
    /// <param name="maxAllowedTokens">
    /// The maximum number of tokens the reader is allowed to read before throwing a
    /// <see cref="SyntaxException"/>. Defaults to <see cref="int.MaxValue"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="sourceText"/> is empty.
    /// </exception>
    public Utf8GraphQLReader(ReadOnlySequence<byte> sourceText, int maxAllowedTokens = int.MaxValue)
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
        _length = checked((int)sourceText.Length);
        _nextNewLines = 0;
        _position = 0;
        _value = null;
        _floatFormat = null;
        _sequence = sourceText;
        _rentedBuffer = null;

        if (sourceText.IsSingleSegment)
        {
            _isMultiSegment = false;
            _sourceText = sourceText.First.Span;
            _currentSpan = _sourceText;
            _currentSpanIndex = 0;
            _segmentOffset = 0;
            _nextSegmentPosition = default;
        }
        else
        {
            _isMultiSegment = true;
            _sourceText = default;
            _nextSegmentPosition = sourceText.Start;
            _sequence.TryGet(ref _nextSegmentPosition, out var firstMemory, advance: true);
            _currentSpan = firstMemory.Span;
            _currentSpanIndex = 0;
            _segmentOffset = 0;

            // Skip any leading empty segments.
            if (_currentSpan.Length == 0)
            {
                AdvanceToNextSpan();
            }
        }
    }

    /// <summary>
    /// Reads the next token in multi-segment mode.
    /// </summary>
    private bool ReadMultiSegment()
    {
        _floatFormat = null;

        if (_position == 0)
        {
            SkipBomMultiSegment();
        }

        SkipWhitespacesMultiSegment();
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

        var code = _currentSpan[_currentSpanIndex];

        if (code.IsPunctuator())
        {
            ReadPunctuatorTokenMultiSegment(code);
            return true;
        }

        if (code.IsLetterOrUnderscore())
        {
            ReadNameTokenMultiSegment();
            return true;
        }

        if (code.IsDigitOrMinus())
        {
            ReadNumberTokenMultiSegment(code);
            return true;
        }

        if (code is GraphQLCharacters.Hash)
        {
            ReadCommentTokenMultiSegment();
            return true;
        }

        if (code is GraphQLCharacters.Quote)
        {
            if (_length > _position + 2
                && PeekByteMs(1) is GraphQLCharacters.Quote
                && PeekByteMs(2) is GraphQLCharacters.Quote)
            {
                AdvanceMultiSegment();
                AdvanceMultiSegment();
                ReadBlockStringTokenMultiSegment();
            }
            else
            {
                ReadStringValueTokenMultiSegment();
            }
            return true;
        }

        throw new SyntaxException(this, UnexpectedCharacter, (char)code, code);
    }

    /// <summary>
    /// Advances the multi-segment position by one byte.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AdvanceMultiSegment()
    {
        _position++;
        _currentSpanIndex++;

        if ((uint)_currentSpanIndex >= (uint)_currentSpan.Length)
        {
            AdvanceToNextSpan();
        }
    }

    /// <summary>
    /// Advances to the next non-empty segment in the sequence.
    /// </summary>
    private bool AdvanceToNextSpan()
    {
        _segmentOffset += _currentSpan.Length;

        if (_sequence.TryGet(ref _nextSegmentPosition, out var memory, advance: true))
        {
            _currentSpan = memory.Span;
            _currentSpanIndex = 0;

            if (_currentSpan.Length == 0)
            {
                return AdvanceToNextSpan();
            }

            return true;
        }

        _currentSpan = default;
        _currentSpanIndex = 0;
        return false;
    }

    /// <summary>
    /// Peeks at a byte at the given offset from the current position,
    /// handling cross-segment boundaries.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte PeekByteMs(int offset)
    {
        var idx = _currentSpanIndex + offset;

        if ((uint)idx < (uint)_currentSpan.Length)
        {
            return _currentSpan[idx];
        }

        return PeekByteMsSlow(idx - _currentSpan.Length);
    }

    /// <summary>
    /// Slow path for cross-segment peek. Uses a local copy of
    /// <see cref="_nextSegmentPosition"/> to avoid mutating state.
    /// </summary>
    private byte PeekByteMsSlow(int remaining)
    {
        var nextPos = _nextSegmentPosition;

        while (_sequence.TryGet(ref nextPos, out var memory, advance: true))
        {
            var span = memory.Span;

            if (span.Length == 0)
            {
                continue;
            }

            if (remaining < span.Length)
            {
                return span[remaining];
            }

            remaining -= span.Length;
        }

        // Past the end of the sequence; return Space as a sentinel
        // (consistent with single-segment ReadDigits behavior at EOF).
        return GraphQLCharacters.Space;
    }

    /// <summary>
    /// Returns a byte at the given absolute position within the sequence,
    /// handling cross-segment lookup.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte GetByteAtPosition()
        => _currentSpan[_currentSpanIndex];

    /// <summary>
    /// Checks whether there is at least one more byte available
    /// in the multi-segment stream at the current position.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool HasMoreDataMultiSegment()
        => _position < _length;

    /// <summary>
    /// Ensures the rented buffer is large enough to hold the requested number of bytes.
    /// </summary>
    private void EnsureBuffer(int minSize)
    {
        if (_rentedBuffer is not null && _rentedBuffer.Length >= minSize)
        {
            return;
        }

        var oldBuffer = _rentedBuffer;
        _rentedBuffer = ArrayPool<byte>.Shared.Rent(minSize);

        if (oldBuffer is not null)
        {
            ArrayPool<byte>.Shared.Return(oldBuffer);
        }
    }

    /// <summary>
    /// Gets the token value as a contiguous span of bytes.
    /// If the token resides entirely within the current segment, a slice is returned directly.
    /// Otherwise, the bytes are linearized into <see cref="_rentedBuffer"/>.
    /// </summary>
    private ReadOnlySpan<byte> GetTokenValue(int absoluteStart, int length)
    {
        var startInSpan = absoluteStart - _segmentOffset;

        if (startInSpan >= 0 && startInSpan + length <= _currentSpan.Length)
        {
            return _currentSpan.Slice(startInSpan, length);
        }

        return LinearizeToken(absoluteStart, length);
    }

    /// <summary>
    /// Copies bytes from the sequence into <see cref="_rentedBuffer"/> and returns
    /// a span over the copied data.
    /// </summary>
    private ReadOnlySpan<byte> LinearizeToken(int absoluteStart, int length)
    {
        EnsureBuffer(length);
        _sequence.Slice(absoluteStart, length).CopyTo(_rentedBuffer);
        return _rentedBuffer.AsSpan(0, length);
    }

    /// <summary>
    /// Disposes of any rented buffer, returning it to the pool.
    /// </summary>
    public void Dispose()
    {
        if (_rentedBuffer is not null)
        {
            ArrayPool<byte>.Shared.Return(_rentedBuffer);
            _rentedBuffer = null;
        }
    }

    /// <summary>
    /// Skips the UTF-8 BOM or UTF-16 BOM in multi-segment mode.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SkipBomMultiSegment()
    {
        var code = _currentSpan[_currentSpanIndex];

        if (code is 239)
        {
            if (PeekByteMs(1) is 187 && PeekByteMs(2) is 191)
            {
                AdvanceMultiSegment();
                AdvanceMultiSegment();
                AdvanceMultiSegment();
            }
        }

        if (code is 254)
        {
            if (PeekByteMs(1) is 255)
            {
                AdvanceMultiSegment();
                AdvanceMultiSegment();
            }
        }
    }

    /// <summary>
    /// Skips whitespace characters (spaces, tabs, commas, newlines) in multi-segment mode.
    /// </summary>
    private void SkipWhitespacesMultiSegment()
    {
        if (_nextNewLines > 0)
        {
            NewLine(_nextNewLines);
            _nextNewLines = 0;
        }

        while (!IsEndOfStream())
        {
            var code = _currentSpan[_currentSpanIndex];

            switch (code)
            {
                case GraphQLCharacters.LineFeed:
                    AdvanceMultiSegment();
                    NewLine();
                    break;

                case GraphQLCharacters.Return:
                    AdvanceMultiSegment();

                    if (!IsEndOfStream()
                        && _currentSpan[_currentSpanIndex] is GraphQLCharacters.LineFeed)
                    {
                        AdvanceMultiSegment();
                    }
                    NewLine();
                    break;

                case GraphQLCharacters.HorizontalTab:
                case GraphQLCharacters.Space:
                case GraphQLCharacters.Comma:
                    AdvanceMultiSegment();
                    break;

                default:
                    return;
            }
        }
    }

    /// <summary>
    /// Reads a punctuator token in multi-segment mode.
    /// </summary>
    private void ReadPunctuatorTokenMultiSegment(byte code)
    {
        _start = _position;
        AdvanceMultiSegment();
        _end = _position;
        _value = null;

        if (code is GraphQLCharacters.Dot)
        {
            if (!IsEndOfStream() && _currentSpan[_currentSpanIndex] is GraphQLCharacters.Dot)
            {
                if (PeekByteMs(1) is GraphQLCharacters.Dot)
                {
                    AdvanceMultiSegment();
                    AdvanceMultiSegment();
                    _end = _position;
                    _kind = TokenKind.Spread;
                }
                else
                {
                    _position = _start;
                    _currentSpanIndex = _start - _segmentOffset;
                    throw ThrowHelper.Reader_InvalidToken(this, TokenKind.Spread);
                }
            }
            else if (!IsEndOfStream() && _currentSpan[_currentSpanIndex].IsDigit())
            {
                _position = _start;
                _currentSpanIndex = _start - _segmentOffset;
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
    /// Reads a name token in multi-segment mode.
    /// [_A-Za-z][_0-9A-Za-z]*
    /// </summary>
    private void ReadNameTokenMultiSegment()
    {
        var start = _position;

ReadNameTokenMultiSegment_Next:
        AdvanceMultiSegment();

        if (!IsEndOfStream() && _currentSpan[_currentSpanIndex].IsLetterOrDigitOrUnderscore())
        {
            goto ReadNameTokenMultiSegment_Next;
        }

        _kind = TokenKind.Name;
        _start = start;
        _end = _position;
        _value = GetTokenValue(start, _position - start);
    }

    /// <summary>
    /// Reads a number token (integer or float) in multi-segment mode.
    /// </summary>
    private void ReadNumberTokenMultiSegment(byte firstCode)
    {
        var start = _position;
        var code = firstCode;
        var isFloat = false;

        if (code is GraphQLCharacters.Minus)
        {
            AdvanceMultiSegment();

            if (IsEndOfStream())
            {
                throw new SyntaxException(this, InvalidNumber, (char)GraphQLCharacters.Space, GraphQLCharacters.Space);
            }

            code = _currentSpan[_currentSpanIndex];
        }

        if (code is GraphQLCharacters.Zero && !IsEndOfStream(_position + 1))
        {
            AdvanceMultiSegment();

            if (!IsEndOfStream())
            {
                code = _currentSpan[_currentSpanIndex];

                if (code.IsDigit())
                {
                    throw new SyntaxException(this, UnexpectedDigit, (char)code, code);
                }
            }
            else
            {
                code = GraphQLCharacters.Space;
            }
        }
        else
        {
            code = ReadDigitsMultiSegment(code);
        }

        if (code is GraphQLCharacters.Dot)
        {
            isFloat = true;
            _floatFormat = Language.FloatFormat.FixedPoint;
            AdvanceMultiSegment();

            if (IsEndOfStream())
            {
                throw new SyntaxException(this, InvalidNumber, (char)GraphQLCharacters.Space, GraphQLCharacters.Space);
            }

            code = _currentSpan[_currentSpanIndex];
            code = ReadDigitsMultiSegment(code);
        }

        const byte lowerCaseBit = 0x20;
        if ((code | lowerCaseBit) is GraphQLCharacters.E)
        {
            isFloat = true;
            _floatFormat = Language.FloatFormat.Exponential;
            AdvanceMultiSegment();

            if (IsEndOfStream())
            {
                throw new SyntaxException(this, InvalidNumber, (char)GraphQLCharacters.Space, GraphQLCharacters.Space);
            }

            code = _currentSpan[_currentSpanIndex];

            if (code is GraphQLCharacters.Plus or GraphQLCharacters.Minus)
            {
                AdvanceMultiSegment();

                if (IsEndOfStream())
                {
                    throw new SyntaxException(this, InvalidNumber, (char)GraphQLCharacters.Space, GraphQLCharacters.Space);
                }

                code = _currentSpan[_currentSpanIndex];
            }

            code = ReadDigitsMultiSegment(code);
        }

        // Lookahead for NameStart.
        // https://github.com/graphql/graphql-spec/pull/601
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
        _value = GetTokenValue(start, _position - start);
    }

    /// <summary>
    /// Reads consecutive digit characters in multi-segment mode.
    /// Returns the first non-digit byte encountered (or Space at end of stream).
    /// </summary>
    private byte ReadDigitsMultiSegment(byte firstCode)
    {
        if (!firstCode.IsDigit())
        {
            throw new SyntaxException(this, InvalidNumber, (char)firstCode, firstCode);
        }

        byte code;

        while (true)
        {
            AdvanceMultiSegment();

            if (IsEndOfStream())
            {
                code = GraphQLCharacters.Space;
                break;
            }

            code = _currentSpan[_currentSpanIndex];

            if (!code.IsDigit())
            {
                break;
            }
        }

        return code;
    }

    /// <summary>
    /// Reads a comment token in multi-segment mode.
    /// #[\u0009\u0020-\uFFFF]*
    /// </summary>
    private void ReadCommentTokenMultiSegment()
    {
        var start = _position;
        var trimStart = _position + 1;
        var trim = true;

        while (true)
        {
            AdvanceMultiSegment();

            if (IsEndOfStream())
            {
                break;
            }

            var code = _currentSpan[_currentSpanIndex];

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
        _value = GetTokenValue(trimStart, _position - trimStart);
    }

    /// <summary>
    /// Reads a string value token in multi-segment mode.
    /// "([^"\\\u000A\u000D]|(\\(u[0-9a-fA-F]{4}|["\\/bfnrt])))*"
    /// </summary>
    private void ReadStringValueTokenMultiSegment()
    {
        var start = _position;

        while (true)
        {
            AdvanceMultiSegment();

            if (IsEndOfStream())
            {
                break;
            }

            var code = _currentSpan[_currentSpanIndex];

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
                    _value = GetTokenValue(
                        start + 1,
                        _position - start - 1);
                    AdvanceMultiSegment();
                    return;

                case GraphQLCharacters.Backslash:
                    AdvanceMultiSegment();

                    if (IsEndOfStream())
                    {
                        throw new SyntaxException(this, UnterminatedString);
                    }

                    code = _currentSpan[_currentSpanIndex];

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
    /// Reads a block string token in multi-segment mode.
    /// The opening """ has already been consumed (position is past the third quote).
    /// </summary>
    private void ReadBlockStringTokenMultiSegment()
    {
        var start = _position - 2;
        _nextNewLines = 0;

        while (true)
        {
            AdvanceMultiSegment();

            if (IsEndOfStream())
            {
                break;
            }

            var code = _currentSpan[_currentSpanIndex];

            switch (code)
            {
                case GraphQLCharacters.LineFeed:
                    _nextNewLines++;
                    break;

                case GraphQLCharacters.Return:
                    if (!IsEndOfStream(_position + 1)
                        && PeekByteMs(1) is GraphQLCharacters.LineFeed)
                    {
                        AdvanceMultiSegment();
                    }
                    _nextNewLines++;
                    break;

                // Closing Triple-Quote (""")
                case GraphQLCharacters.Quote:
                    if (PeekByteMs(1) is GraphQLCharacters.Quote
                        && PeekByteMs(2) is GraphQLCharacters.Quote)
                    {
                        _kind = TokenKind.BlockString;
                        _start = start;
                        _end = _position + 2;
                        _value = GetTokenValue(
                            start + 3,
                            _position - start - 3);
                        // Advance past the three closing quotes.
                        AdvanceMultiSegment();
                        AdvanceMultiSegment();
                        AdvanceMultiSegment();
                        return;
                    }
                    break;

                case GraphQLCharacters.Backslash:
                    if (PeekByteMs(1) is GraphQLCharacters.Quote
                        && PeekByteMs(2) is GraphQLCharacters.Quote
                        && PeekByteMs(3) is GraphQLCharacters.Quote)
                    {
                        AdvanceMultiSegment();
                        AdvanceMultiSegment();
                        AdvanceMultiSegment();
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
}
