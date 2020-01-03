using System.Runtime.InteropServices;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using StrawberryShake.Language.Properties;

namespace StrawberryShake.VisualStudio.Language
{
    public ref partial struct TextGraphQLParser
    {
        internal TokenKind Kind => _reader.Kind;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal NameNode ParseName()
        {
            ISyntaxToken start = _reader.Token;
            string name = ExpectName();
            var location = new Location(start, _reader.Token);

            return new NameNode
            (
                location,
                name
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool MoveNext() => _reader.MoveNext();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe string ExpectName()
        {
            if (_reader.Kind == TokenKind.Name)
            {
                fixed (char* c = _reader.Value)
                {
                    string name = new string(c);
                    MoveNext();
                    return name;
                }
            }

            throw new SyntaxException(_reader,
                string.Format(CultureInfo.InvariantCulture,
                    LangResources.Parser_InvalidToken,
                    TokenKind.Name,
                    _reader.Kind));
        }

        internal void ExpectColon() =>
            Expect(TokenKind.Colon);

        internal void ExpectDollar() =>
            Expect(TokenKind.Dollar);

        private void ExpectAt() =>
            Expect(TokenKind.At);

        private void ExpectRightBracket() =>
            Expect(TokenKind.RightBracket);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe string ExpectString()
        {
            if (TokenHelper.IsString(in _reader))
            {
                fixed (char* c = _reader.Value)
                {
                    string value = new string(c);
                    MoveNext();
                    return value;
                }
            }

            throw new SyntaxException(_reader,
                string.Format(CultureInfo.InvariantCulture,
                    LangResources.Parser_InvalidToken,
                    TokenKind.String,
                    _reader.Kind));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe string ExpectScalarValue()
        {
            if (TokenHelper.IsScalarValue(in _reader))
            {
                fixed (char* c = _reader.Value)
                {
                    string value = new string(c);
                    MoveNext();
                    return value;
                }
            }

            throw new SyntaxException(_reader,
                string.Format(CultureInfo.InvariantCulture,
                    LangResources.Parser_InvalidScalarToken,
                    _reader.Kind));
        }

        private void ExpectSpread() => Expect(TokenKind.Spread);

        internal void ExpectRightParenthesis() => Expect(TokenKind.RightParenthesis);

        private void ExpectRightBrace() => Expect(TokenKind.RightBrace);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Expect(TokenKind kind)
        {
            if (!_reader.Skip(kind))
            {
                throw new SyntaxException(_reader,
                    string.Format(CultureInfo.InvariantCulture,
                        LangResources.Parser_InvalidToken,
                        kind,
                        _reader.Kind));
            }
        }

        private void ExpectDirectiveKeyword() => ExpectKeyword(GraphQLKeywords.Directive);

        private void ExpectOnKeyword() => ExpectKeyword(GraphQLKeywords.On);

        private void ExpectFragmentKeyword() => ExpectKeyword(GraphQLKeywords.Fragment);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void ExpectKeyword(ReadOnlySpan<char> keyword)
        {
            if (!SkipKeyword(keyword))
            {
                fixed (char* c = _reader.Value)
                fixed (char* k = keyword)
                {
                    string found = _reader.Kind == TokenKind.Name
                        ? new string(c)
                        : _reader.Kind.ToString();

                    throw new SyntaxException(_reader,
                        string.Format(CultureInfo.InvariantCulture,
                            LangResources.Parser_InvalidToken,
                            new string(k),
                            found));
                }
            }
        }

        private bool SkipPipe() => _reader.Skip(TokenKind.Pipe);

        private bool SkipEqual() => _reader.Skip(TokenKind.Equal);

        private bool SkipColon() => _reader.Skip(TokenKind.Colon);

        private bool SkipAmpersand() => _reader.Skip(TokenKind.Ampersand);

        private bool SkipRepeatableKeyword() => SkipKeyword(GraphQLKeywords.Repeatable);

        private bool SkipImplementsKeyword() => SkipKeyword(GraphQLKeywords.Implements);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SkipKeyword(ReadOnlySpan<char> keyword)
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

        private SyntaxException Unexpected(TokenKind kind)
        {
            return new SyntaxException(_reader,
                $"Unexpected token: {TokenVisualizer.Visualize(kind)}.");
        }
    }
}
