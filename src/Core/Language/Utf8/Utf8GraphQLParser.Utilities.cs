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
            TokenInfo start = Start();
            string name = ExpectName();
            Location location = CreateLocation(in start);

            return new NameNode
            (
                location,
                name
            );
        }

        private bool MoveNext()
        {
            while (_reader.Read() && _reader.Kind == TokenKind.Comment) ;
            return !_reader.IsEndOfStream();
        }

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
        private Location CreateLocation(in TokenInfo start) =>
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string ExpectScalarValue()
        {
            if (TokenHelper.IsScalarValue(in _reader))
            {
                string value = _reader.GetScalarValue();
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

        private void ExpectRightParenthesis() =>
            Expect(TokenKind.RightParenthesis);

        private void ExpectRightBrace() =>
            Expect(TokenKind.RightBrace);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        private bool SkipPipe() => Skip(TokenKind.Pipe);

        private bool SkipEqual() => Skip(TokenKind.Equal);

        private bool SkipColon() => Skip(TokenKind.Colon);

        private bool SkipAmpersand() => Skip(TokenKind.Ampersand);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Skip(TokenKind kind)
        {
            if (_reader.Kind == kind)
            {
                MoveNext();
                return true;
            }
            return false;
        }

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
