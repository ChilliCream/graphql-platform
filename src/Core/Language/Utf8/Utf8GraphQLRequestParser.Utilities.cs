using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLRequestParser
    {
        private string ParseStringOrNull()
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

            throw new SyntaxException(_reader, "RESOURCES");
        }

        private IReadOnlyDictionary<string, object> ParseObjectOrNull()
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

            throw new SyntaxException(_reader, "RESOURCES");
        }
    }
}
