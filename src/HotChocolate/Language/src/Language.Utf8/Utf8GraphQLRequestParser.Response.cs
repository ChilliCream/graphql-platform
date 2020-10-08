using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language.Properties;
using System.Runtime.CompilerServices;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLRequestParser
    {
        private object ParseResponseValue()
        {
            return _reader.Kind switch
            {
                TokenKind.LeftBracket => ParseResponseList(),
                TokenKind.LeftBrace => ParseResponseObject(),
                TokenKind.String => ParseScalarSyntax(),
                TokenKind.Integer => ParseScalarSyntax(),
                TokenKind.Float => ParseScalarSyntax(),
                TokenKind.Name => ParseScalarSyntax(),
                _ => throw ThrowHelper.UnexpectedToken(_reader)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Dictionary<string, object?> ParseResponseObject()
        {
            _reader.Expect(TokenKind.LeftBrace);

            var fields = new Dictionary<string, object?>();

            while (_reader.Kind != TokenKind.RightBrace)
            {
                ParseResponseObjectFieldSyntax(fields);
            }

            // skip closing token
            _reader.Expect(TokenKind.RightBrace);

            return fields;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseResponseObjectFieldSyntax(
            Dictionary<string, object?> fields)
        {
            if (_reader.Kind != TokenKind.String)
            {
                throw new SyntaxException(_reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        TokenKind.String,
                        TokenVisualizer.Visualize(in _reader)));
            }

            string name = _reader.GetString();
            _reader.MoveNext();
            _reader.Expect(TokenKind.Colon);
            object value = ParseResponseValue();

            fields[name] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private List<object> ParseResponseList()
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
                list.Add(ParseResponseValue());
            }

            // skip closing token
            _reader.Expect(TokenKind.RightBracket);

            return list;
        }
    }
}
