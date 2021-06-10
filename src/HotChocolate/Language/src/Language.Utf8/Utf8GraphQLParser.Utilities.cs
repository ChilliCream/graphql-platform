using System;
using System.Runtime.CompilerServices;
using static HotChocolate.Language.Utf8GraphQLReader;
using static HotChocolate.Language.TokenPrinter;
using static HotChocolate.Language.Properties.LangUtf8Resources;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLParser
    {
        internal TokenKind Kind => _reader.Kind;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal NameNode ParseName()
        {
            TokenInfo start = Start();
            string name = ExpectName();
            Location? location = CreateLocation(in start);

            return new NameNode
            (
                location,
                name
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool MoveNext() => _reader.MoveNext();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TokenInfo Start() =>
            _createLocation
                ? new TokenInfo(
                    _reader.Start,
                    _reader.End,
                    _reader.Line,
                    _reader.Column)
                : default;

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
                string name = _reader.GetName();
                MoveNext();
                return name;
            }

            throw new SyntaxException(_reader, Parser_InvalidToken, TokenKind.Name, _reader.Kind);
        }

        internal void ExpectColon() => Expect(TokenKind.Colon);

        internal void ExpectDollar() => Expect(TokenKind.Dollar);

        private void ExpectAt() => Expect(TokenKind.At);

        private void ExpectRightBracket() => Expect(TokenKind.RightBracket);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string ExpectString()
        {
            if (TokenHelper.IsString(in _reader))
            {
                string value = _reader.GetString();
                MoveNext();
                return value;
            }

            throw new SyntaxException(_reader, Parser_InvalidToken, TokenKind.String, _reader.Kind);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Memory<byte> ExpectScalarValue()
        {
            if (TokenHelper.IsScalarValue(in _reader))
            {
                Memory<byte> value = _reader.Value.ToArray();
                MoveNext();
                return value;
            }

            throw new SyntaxException(_reader, Parser_InvalidScalarToken, _reader.Kind);
        }

        private void ExpectSpread() => Expect(TokenKind.Spread);

        internal void ExpectRightParenthesis() => Expect(TokenKind.RightParenthesis);

        private void ExpectRightBrace() => Expect(TokenKind.RightBrace);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Expect(TokenKind kind)
        {
            if (!_reader.Skip(kind))
            {
                throw new SyntaxException(_reader, Parser_InvalidToken, kind, _reader.Kind);
            }
        }

        private void ExpectDirectiveKeyword() => ExpectKeyword(GraphQLKeywords.Directive);

        private void ExpectOnKeyword() => ExpectKeyword(GraphQLKeywords.On);

        private void ExpectFragmentKeyword() => ExpectKeyword(GraphQLKeywords.Fragment);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExpectKeyword(ReadOnlySpan<byte> keyword)
        {
            if (!SkipKeyword(keyword))
            {
                string found = _reader.Kind == TokenKind.Name
                    ? _reader.GetName()
                    : _reader.Kind.ToString();

                throw new SyntaxException(_reader, Parser_InvalidToken, GetString(keyword), found);
            }
        }

        private bool SkipPipe() => _reader.Skip(TokenKind.Pipe);

        private bool SkipEqual() => _reader.Skip(TokenKind.Equal);

        private bool SkipColon() => _reader.Skip(TokenKind.Colon);

        private bool SkipAmpersand() => _reader.Skip(TokenKind.Ampersand);

        private bool SkipRepeatableKeyword() =>
            SkipKeyword(GraphQLKeywords.Repeatable);

        private bool SkipImplementsKeyword() =>
            SkipKeyword(GraphQLKeywords.Implements);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SkipKeyword(ReadOnlySpan<byte> keyword)
        {
            if (_reader.Kind == TokenKind.Name
                && _reader.Value.SequenceEqual(keyword))
            {
                MoveNext();
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private StringValueNode? TakeDescription()
        {
            StringValueNode? description = _description;
            _description = null;
            return description;
        }

        private SyntaxException Unexpected(TokenKind kind) =>
            new(_reader, UnexpectedToken, Print(kind));
    }
}
