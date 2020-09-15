using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLRequestParser
    {
        private string? ParseStringOrNull()
        {
            switch (_reader.Kind)
            {
                case TokenKind.String:
                {
                    string value = _reader.GetString();
                    _reader.MoveNext();
                    return value;
                }
                case TokenKind.Name when _reader.Value.SequenceEqual(GraphQLKeywords.Null):
                    _reader.MoveNext();
                    return null;

                default:
                    throw ThrowHelper.ExpectedStringOrNull(_reader);
            }
        }

        private IReadOnlyDictionary<string, object?>? ParseObjectOrNull(bool preserveNumbers)
        {
            switch (_reader.Kind)
            {
                case TokenKind.LeftBrace:
                    return ParseObject(preserveNumbers);

                case TokenKind.Name when _reader.Value.SequenceEqual(GraphQLKeywords.Null):
                    _reader.MoveNext();
                    return null;

                default:
                    throw ThrowHelper.ExpectedObjectOrNull(_reader);
            }
        }

        private bool IsNullToken()
        {
            return _reader.Kind == TokenKind.Name
                && _reader.Value.SequenceEqual(GraphQLKeywords.Null);
        }
    }
}
