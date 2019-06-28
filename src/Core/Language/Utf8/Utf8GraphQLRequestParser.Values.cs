using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language.Properties;
using System.Runtime.CompilerServices;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLRequestParser
    {
        private object ParseValue()
        {
            switch (_reader.Kind)
            {
                case TokenKind.LeftBracket:
                    return ParseList();

                case TokenKind.LeftBrace:
                    return ParseObject();

                case TokenKind.String:
                case TokenKind.Integer:
                case TokenKind.Float:
                case TokenKind.Name:
                    return ParseScalar();

                default:
                    throw new SyntaxException(_reader, "RESOURCES");
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IReadOnlyDictionary<string, object> ParseObject()
        {
            if (_reader.Kind != TokenKind.LeftBrace)
            {
                throw new SyntaxException(_reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        TokenKind.LeftBrace,
                        TokenVisualizer.Visualize(in _reader)));
            }

            _reader.Expect(TokenKind.LeftBrace);

            var obj = new Dictionary<string, object>();

            while (_reader.Kind != TokenKind.RightBrace)
            {
                ParseObjectField(obj);
            }

            // skip closing token
            _reader.Expect(TokenKind.RightBrace);

            return obj;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseObjectField(IDictionary<string, object> obj)
        {
            if (_reader.Kind != TokenKind.String)
            {
                throw new SyntaxException(_reader, "RESOURCES");
            }

            string name = _reader.GetString();
            _reader.MoveNext();
            _reader.Expect(TokenKind.Colon);
            object value = ParseValue();
            obj.Add(name, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IReadOnlyList<object> ParseList()
        {
            if (_reader.Kind != TokenKind.LeftBracket)
            {
                throw new SyntaxException(_reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        TokenKind.LeftBracket,
                        TokenVisualizer.Visualize(in _reader)));
            }

            var list = new List<object>();

            // skip opening token
            _reader.MoveNext();

            while (_reader.Kind != TokenKind.RightBracket)
            {
                list.Add(ParseValue());
            }

            // skip closing token
            _reader.Expect(TokenKind.RightBracket);

            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object ParseScalar()
        {
            string value = null;

            switch (_reader.Kind)
            {
                case TokenKind.String:
                    value = _reader.GetString();
                    _reader.MoveNext();
                    return value;

                case TokenKind.Integer:
                    value = _reader.GetScalarValue();
                    _reader.MoveNext();
                    return long.Parse(value);

                case TokenKind.Float:
                    value = _reader.GetScalarValue();
                    _reader.MoveNext();
                    return decimal.Parse(value);

                case TokenKind.Name:
                    if (_reader.Value.SequenceEqual(GraphQLKeywords.True))
                    {
                        _reader.MoveNext();
                        return true;
                    }

                    if (_reader.Value.SequenceEqual(GraphQLKeywords.False))
                    {
                        _reader.MoveNext();
                        return false;
                    }

                    if (_reader.Value.SequenceEqual(GraphQLKeywords.Null))
                    {
                        _reader.MoveNext();
                        return null;
                    }
                    break;
            }

            throw new SyntaxException(_reader, "RESOURCES");
        }
    }
}
