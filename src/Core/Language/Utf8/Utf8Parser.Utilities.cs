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
        private static NameNode ParseName(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);
            string name = ParserHelper.ExpectName(ref reader);
            Location location = context.CreateLocation(ref reader);

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

        private static string ExpectName(ref Utf8GraphQLReader reader)
        {
            if (reader.Kind == TokenKind.Name)
            {
                string name = reader.GetString(reader.Value);
                MoveNext(ref reader);
                return name;
            }

            throw new SyntaxException(reader,
                $"Expected a name token: {TokenVisualizer.Visualize(in reader)}.");
        }

        private static void ExpectColon(ref Utf8GraphQLReader reader)
        {
            Expect(ref reader, TokenKind.Colon);
        }

        private static void ExpectDollar(ref Utf8GraphQLReader reader)
        {
            Expect(ref reader, TokenKind.Dollar);
        }

        private static void ExpectAt(ref Utf8GraphQLReader reader)
        {
            Expect(ref reader, TokenKind.At);
        }

        private static void ExpectRightBracket(ref Utf8GraphQLReader reader)
        {
            Expect(TokenKind.RightBracket);
        }

        private string ExpectString()
        {
            if (TokenHelper.IsString(in _reader))
            {
                string value = _reader.GetString();
                MoveNext();
                return value;
            }

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

            throw new SyntaxException(_reader,
                "Expected a scalar value token: " +
                $"{TokenVisualizer.Visualize(in _reader)}.");
        }

        private void ExpectSpread(ref Utf8GraphQLReader reader)
        {
            Expect(TokenKind.Spread);
        }

        private void Expect(TokenKind kind)
        {
            if (!Skip(kind))
            {
                throw new SyntaxException(reader,
                    $"Expected a name token: {reader.Kind}.");
            }
        }

        private void ExpectSchemaKeyword()
        {
            ExpectKeyword(GraphQLKeywords.Schema);
        }

        private void ExpectDirectiveKeyword()
        {
            ExpectKeyword(GraphQLKeywords.Directive);
        }

        private void ExpectOnKeyword()
        {
            ExpectKeyword(GraphQLKeywords.On);
        }

        private void ExpectFragmentKeyword()
        {
            ExpectKeyword(GraphQLKeywords.Fragment);
        }

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

        private bool Skip(TokenKind kind)
        {
            if (_reader.Kind == kind)
            {
                return MoveNext();
            }
            return false;
        }

        private bool SkipRepeatableKeyword()
        {
            return SkipKeyword(GraphQLKeywords.Repeatable);
        }

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

        private SyntaxException Unexpected(TokenKind kind)
        {
            return new SyntaxException(_reader,
                $"Unexpected token: {TokenVisualizer.Visualize(kind)}.");
        }
    }
}
