using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using StrawberryShake.VisualStudio.Language.Properties;

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


        private void ParseName(SyntaxClassificationKind kind)
        {
            if (_reader.Kind == TokenKind.Name)
            {
                _classifications.AddClassification(
                    kind,
                    _reader.Token);
            }
            else
            {
                _classifications.AddClassification(
                    SyntaxClassificationKind.Error,
                    _reader.Token);
            }
            MoveNext();
        }

        private bool MoveNext()
        {
            while (_reader.Read())
            {
                if (_reader.Kind == TokenKind.Comment)
                {
                    _classifications.AddClassification(
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

        private void ParseColon() =>
            Expect(SyntaxClassificationKind.Colon, TokenKind.Colon);

        private void ExpectAt() =>
            Expect(SyntaxClassificationKind.At, TokenKind.At);

        private void ParseRightBracket() =>
            Expect(SyntaxClassificationKind.Bracket, TokenKind.RightBracket);

        private void ParseSpread() =>
            Expect(SyntaxClassificationKind.Spread, TokenKind.Spread);

        private void ParseRightParenthesis() =>
            Expect(SyntaxClassificationKind.Parenthesis, TokenKind.RightParenthesis);

        private void ParseRightBrace() =>
            Expect(SyntaxClassificationKind.Brace, TokenKind.RightBrace);

        private void Expect(
            SyntaxClassificationKind classificationKind,
            TokenKind tokenKind)
        {
            if (_reader.Skip(tokenKind, out ISyntaxToken? token))
            {
                _classifications.AddClassification(
                    classificationKind,
                    token);
            }
            else
            {
                _classifications.AddClassification(
                    SyntaxClassificationKind.Error,
                    _reader.Token);
                MoveNext();
            }
        }

        private void ParseDirectiveKeyword() =>
            ParseKeyword(SyntaxClassificationKind.DirectiveKeyword, GraphQLKeywords.Directive);

        private void ParseOnKeyword() =>
            ParseKeyword(SyntaxClassificationKind.OnKeyword, GraphQLKeywords.On);

        private void ParseFragmentKeyword() =>
            ParseKeyword(SyntaxClassificationKind.FragmentKeyword, GraphQLKeywords.Fragment);


        private void ParseKeyword(
            SyntaxClassificationKind classificationKind,
            ReadOnlySpan<char> keyword)
        {
            if (_reader.Kind == TokenKind.Name
                && _reader.Value.SequenceEqual(keyword))
            {
                _classifications.AddClassification(
                    classificationKind,
                    _reader.Token);
            }
            else
            {
                _classifications.AddClassification(
                    SyntaxClassificationKind.Error,
                    _reader.Token);
            }
            MoveNext();
        }

        private bool SkipPipe() =>
            Skip(SyntaxClassificationKind.Pipe, TokenKind.Pipe);

        private bool SkipEqual() =>
            Skip(SyntaxClassificationKind.Equal, TokenKind.Equal);

        private bool SkipColon() =>
            Skip(SyntaxClassificationKind.Colon, TokenKind.Colon);

        private bool SkipAmpersand() =>
            Skip(SyntaxClassificationKind.Ampersand, TokenKind.Ampersand);

        private bool SkipImplementsKeyword() =>
            SkipKeyword(SyntaxClassificationKind.ImplementsKeyword, GraphQLKeywords.Implements);

        private bool SkipKeyword(
            SyntaxClassificationKind classificationKind,
            ReadOnlySpan<char> keyword)
        {
            if (_reader.Kind == TokenKind.Name
                && _reader.Value.SequenceEqual(keyword))
            {
                _classifications.AddClassification(
                    classificationKind,
                    _reader.Token);
                MoveNext();
                return true;
            }
            return false;
        }


        private bool Skip(
            SyntaxClassificationKind classificationKind,
            TokenKind tokenKind)
        {
            if (_reader.Kind == tokenKind)
            {
                _classifications.AddClassification(
                    classificationKind,
                    _reader.Token);
                MoveNext();
                return true;
            }
            return false;
        }
    }
}
