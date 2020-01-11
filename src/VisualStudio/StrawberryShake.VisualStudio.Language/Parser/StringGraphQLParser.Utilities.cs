using System.Runtime.InteropServices;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using StrawberryShake.VisualStudio.Language.Properties;

namespace StrawberryShake.VisualStudio.Language
{
    public ref partial struct StringGraphQLParser
    {
        private static readonly bool[] _isString = new bool[22];
        private static readonly bool[] _isScalar = new bool[22];

        static StringGraphQLParser()
        {
            _isString[(int)TokenKind.BlockString] = true;
            _isString[(int)TokenKind.String] = true;

            _isScalar[(int)TokenKind.BlockString] = true;
            _isScalar[(int)TokenKind.String] = true;
            _isScalar[(int)TokenKind.Integer] = true;
            _isScalar[(int)TokenKind.Float] = true;
        }

        private TokenKind Kind => _reader.Kind;


        private NameNode ParseName()
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


        private bool MoveNext() => _reader.MoveNext();


        private unsafe string ExpectName()
        {
            if (_reader.Kind == TokenKind.Name)
            {
                fixed (char* c = _reader.Value)
                {
                    string name = new string(c, 0, _reader.Value.Length);
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

        private void ExpectColon() =>
            Expect(TokenKind.Colon);

        private void ExpectDollar() =>
            Expect(TokenKind.Dollar);

        private void ExpectAt() =>
            Expect(TokenKind.At);

        private void ExpectRightBracket() =>
            Expect(TokenKind.RightBracket);


        private unsafe string ExpectString()
        {
            if (_isString[(int)_reader.Kind])
            {
                fixed (char* c = _reader.Value)
                {
                    string value = new string(c, 0, _reader.Value.Length);
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


        private unsafe string ExpectScalarValue()
        {
            if (_isScalar[(int)_reader.Kind])
            {
                fixed (char* c = _reader.Value)
                {
                    string value = new string(c, 0, _reader.Value.Length);
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

        private void ExpectRightParenthesis() => Expect(TokenKind.RightParenthesis);

        private void ExpectRightBrace() => Expect(TokenKind.RightBrace);


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


        private unsafe void ExpectKeyword(ReadOnlySpan<char> keyword)
        {
            if (!SkipKeyword(keyword))
            {
                fixed (char* c = _reader.Value)
                fixed (char* k = keyword)
                {
                    string found = _reader.Kind == TokenKind.Name
                        ? new string(c, 0, _reader.Value.Length)
                        : _reader.Kind.ToString();

                    throw new SyntaxException(_reader,
                        string.Format(CultureInfo.InvariantCulture,
                            LangResources.Parser_InvalidToken,
                            new string(k, 0, keyword.Length),
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
