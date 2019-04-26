using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLParser
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private NameNode ParseName()
        {
            TokenInfo start = TokenInfo.FromReader(in _reader);
            string name = ExpectName();
            Location location = CreateLocation(in start);

            return new NameNode
            (
                location,
                name
            );
        }

        public bool MoveNext()
        {
            while (_reader.Read() && _reader.Kind == TokenKind.Comment) ;
            return !_reader.IsEndOfStream();
        }

        public Location CreateLocation(in TokenInfo start) =>
            _createLocation
                ? new Location(
                    start.Start,
                    _reader.End,
                    start.Line,
                    start.Column)
                : null;

        private string ExpectName()
        {
            if (_reader.Kind == TokenKind.Name)
            {
                string name = _reader.GetString(_reader.Value);
                MoveNext();
                return name;
            }

            // TODO : resources
            throw new SyntaxException(_reader,
                $"Expected a name token: {TokenVisualizer.Visualize(in _reader)}.");
        }

        private void ExpectColon() =>
            Expect(TokenKind.Colon);

        private void ExpectDollar() =>
            Expect(TokenKind.Dollar);

        private void ExpectAt() =>
            Expect(TokenKind.At);

        private void ExpectRightBracket() =>
            Expect(TokenKind.RightBracket);

        private string ExpectString()
        {
            if (TokenHelper.IsString(in _reader))
            {
                string value = _reader.GetString();
                MoveNext();
                return value;
            }

            // TODO : resources
            throw new SyntaxException(_reader,
                "Expected a string token: " +
                $"{TokenVisualizer.Visualize(in _reader)}.");
        }

        private string ExpectScalarValue()
        {
            if (TokenHelper.IsScalarValue(in _reader))
            {
                string value = _reader.GetString(_reader.Value);
                MoveNext();
                return value;
            }

            // TODO : resources
            throw new SyntaxException(_reader,
                "Expected a scalar value token: " +
                $"{TokenVisualizer.Visualize(in _reader)}.");
        }

        private void ExpectSpread() =>
            Expect(TokenKind.Spread);

        public void ExpectRightParenthesis() =>
            Expect(TokenKind.RightParenthesis);

        public void ExpectRightBrace() =>
            Expect(TokenKind.RightBrace);

        private void Expect(TokenKind kind)
        {
            if (!Skip(kind))
            {
                // TODO : resources
                throw new SyntaxException(_reader,
                    $"Expected a name token: {kind}.");
            }
        }

        private void ExpectSchemaKeyword() =>
            ExpectKeyword(GraphQLKeywords.Schema);

        private void ExpectDirectiveKeyword() =>
            ExpectKeyword(GraphQLKeywords.Directive);

        private void ExpectOnKeyword() =>
            ExpectKeyword(GraphQLKeywords.On);

        private void ExpectFragmentKeyword() =>
            ExpectKeyword(GraphQLKeywords.Fragment);

        private void ExpectKeyword(ReadOnlySpan<byte> keyword)
        {
            if (!SkipKeyword(keyword))
            {
                // TODO : resources
                throw new SyntaxException(_reader,
                    $"Expected \"{Encoding.UTF8.GetString(keyword.ToArray())}\", found " +
                    $"{TokenVisualizer.Visualize(in _reader)}");
            }
        }

        private void SkipDescription()
        {
            if (TokenHelper.IsDescription(in _reader))
            {
                MoveNext();
            }
        }

        private bool SkipPipe() => Skip(TokenKind.Pipe);

        private bool SkipEqual() => Skip(TokenKind.Equal);

        private bool SkipColon() => Skip(TokenKind.Colon);

        private bool SkipAmpersand() => Skip(TokenKind.Ampersand);

        private bool Skip(TokenKind kind)
        {
            if (_reader.Kind == kind)
            {
                return MoveNext();
            }
            return false;
        }

        private bool SkipRepeatableKeyword() =>
            SkipKeyword(GraphQLKeywords.Repeatable);

        private bool SkipImplementsKeyword() =>
            SkipKeyword(GraphQLKeywords.Implements);

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

        private StringValueNode TakeDescription()
        {
            StringValueNode description = _description;
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
