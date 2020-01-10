using System.Runtime.InteropServices;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using StrawberryShake.VisualStudio.Language.Properties;
using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public ref partial struct StringGraphQLClassifier
    {
        private static readonly bool[] _isString = new bool[22];
        private static readonly bool[] _isScalar = new bool[22];

        static StringGraphQLClassifier()
        {
            _isString[(int)TokenKind.BlockString] = true;
            _isString[(int)TokenKind.String] = true;

            _isScalar[(int)TokenKind.BlockString] = true;
            _isScalar[(int)TokenKind.String] = true;
            _isScalar[(int)TokenKind.Integer] = true;
            _isScalar[(int)TokenKind.Float] = true;
        }

        private TokenKind Kind => _reader.Kind;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseName(
            ICollection<SyntaxClassification> classifications,
            SyntaxClassificationKind kind)
        {
            ISyntaxToken start = _reader.Token;
            ExpectName();
            var location = new Location(start, _reader.Token);

            classifications.AddClassification(kind, location);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool MoveNext(ICollection<SyntaxClassification> classifications)
        {
            while (_reader.Read())
            {
                if (_reader.Kind == TokenKind.Comment)
                {
                    classifications.AddClassification(
                        SyntaxClassificationKind.Comment,
                        _reader.Token);
                }
                else
                {
                    break;
                }
            }
            return !_reader.IsEndOfStream();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Expect(
            ICollection<SyntaxClassification> classifications,
            SyntaxClassificationKind classificationKind,
            TokenKind tokenKind)
        {
            if (_reader.Skip(tokenKind))
            {

            }
            else
            {
                classifications.AddClassification(classificationKind, _reader.Token);
                MoveNext();
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
