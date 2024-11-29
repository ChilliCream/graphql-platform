using System.Runtime.CompilerServices;
using static HotChocolate.Language.Properties.LangUtf8Resources;
using static HotChocolate.Language.TokenPrinter;
using static HotChocolate.Language.Utf8GraphQLReader;

namespace HotChocolate.Language;

public ref partial struct Utf8GraphQLParser
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private NameNode ParseName()
    {
        var start = Start();
        var name = ExpectName();
        var location = CreateLocation(in start);

        return new NameNode
        (
            location,
            name
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool MoveNext() => _reader.MoveNext();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TokenInfo Start()
    {
        if (++_parsedNodes > _maxAllowedNodes)
        {
            throw new SyntaxException(
                _reader,
                string.Format(
                    Utf8GraphQLParser_Start_MaxAllowedNodesReached,
                    _maxAllowedNodes));
        }

        return _createLocation
            ? new TokenInfo(
                _reader.Start,
                _reader.End,
                _reader.Line,
                _reader.Column)
            : default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Location? CreateLocation(in TokenInfo start) =>
        _createLocation
            ? new Location(
                start.Start,
                _reader.End,
                start.Line,
                start.Column)
            : null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string ExpectName()
    {
        if (_reader.Kind == TokenKind.Name)
        {
            var name = _reader.GetName();
            MoveNext();
            return name;
        }

        throw new SyntaxException(_reader, Parser_InvalidToken, TokenKind.Name, _reader.Kind);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExpectColon() => Expect(TokenKind.Colon);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExpectDollar() => Expect(TokenKind.Dollar);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExpectAt() => Expect(TokenKind.At);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExpectRightBracket() => Expect(TokenKind.RightBracket);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string ExpectString()
    {
        if (TokenHelper.IsString(ref _reader))
        {
            var value = _reader.GetString();
            MoveNext();
            return value;
        }

        throw new SyntaxException(_reader, Parser_InvalidToken, TokenKind.String, _reader.Kind);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExpectSpread() => Expect(TokenKind.Spread);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExpectRightParenthesis() => Expect(TokenKind.RightParenthesis);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExpectRightBrace() => Expect(TokenKind.RightBrace);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Expect(TokenKind kind)
    {
        if (!_reader.Skip(kind))
        {
            throw new SyntaxException(_reader, Parser_InvalidToken, kind, _reader.Kind);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExpectDirectiveKeyword() => ExpectKeyword(GraphQLKeywords.Directive);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExpectOnKeyword() => ExpectKeyword(GraphQLKeywords.On);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExpectFragmentKeyword() => ExpectKeyword(GraphQLKeywords.Fragment);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExpectKeyword(ReadOnlySpan<byte> keyword)
    {
        if (!SkipKeyword(keyword))
        {
            var found = _reader.Kind == TokenKind.Name
                ? _reader.GetName()
                : _reader.Kind.ToString();

            throw new SyntaxException(_reader, Parser_InvalidToken, GetString(keyword), found);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool SkipPipe() => _reader.Skip(TokenKind.Pipe);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool SkipEqual() => _reader.Skip(TokenKind.Equal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool SkipColon() => _reader.Skip(TokenKind.Colon);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool SkipAmpersand() => _reader.Skip(TokenKind.Ampersand);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool SkipAt() => _reader.Skip(TokenKind.At);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool SkipDot() => _reader.Skip(TokenKind.Dot);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool SkipRepeatableKeyword()
        => SkipKeyword(GraphQLKeywords.Repeatable);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool SkipImplementsKeyword()
        => SkipKeyword(GraphQLKeywords.Implements);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool SkipKeyword(ReadOnlySpan<byte> keyword)
    {
        if (_reader.Kind == TokenKind.Name &&
            _reader.Value.SequenceEqual(keyword))
        {
            MoveNext();
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private StringValueNode? TakeDescription()
    {
        var description = _description;
        _description = null;
        return description;
    }

    private SyntaxException Unexpected(TokenKind kind)
        => new(_reader, UnexpectedToken, Print(kind));
}
