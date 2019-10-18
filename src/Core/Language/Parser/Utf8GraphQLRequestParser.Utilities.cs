using System.Globalization;
using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLRequestParser
    {
        private string? ParseStringOrNull()
        {
            if (_reader.Kind == TokenKind.String)
            {
                string value = _reader.GetString();
                _reader.MoveNext();
                return value;
            }

            if (_reader.Kind == TokenKind.Name
                && _reader.Value.SequenceEqual(GraphQLKeywords.Null))
            {
                _reader.MoveNext();
                return null;
            }

            // TODO : resources
            throw new SyntaxException(
                _reader,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Expected a string-token or a null-token, " +
                    "but found a {0}-token with value `{1}`.",
                    _reader.Kind.ToString(),
                    _reader.GetString()));
        }

        private IReadOnlyDictionary<string, object?>? ParseObjectOrNull()
        {
            if (_reader.Kind == TokenKind.LeftBrace)
            {
                return ParseObject();
            }

            if (_reader.Kind == TokenKind.Name
                && _reader.Value.SequenceEqual(GraphQLKeywords.Null))
            {
                _reader.MoveNext();
                return null;
            }

            // TODO : resources
            throw new SyntaxException(
                _reader,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Expected an object or a null-token, " +
                    "but found a {0}-token with value `{1}`.",
                    _reader.Kind.ToString(),
                    _reader.GetString()));
        }

        private bool IsNullToken()
        {
            return _reader.Kind == TokenKind.Name
                && _reader.Value.SequenceEqual(GraphQLKeywords.Null);
        }

        // TODO : resources
        private SyntaxException UnexpectedToken() =>
            throw new SyntaxException(_reader,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Unexpected token found `{0}` " +
                    "while expecting a scalar value.",
                    _reader.Kind));
    }
}
