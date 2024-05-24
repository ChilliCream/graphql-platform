using System.Diagnostics.CodeAnalysis;
using static HotChocolate.Language.Properties.LangWebResources;

namespace HotChocolate.Language;

public ref partial struct Utf8GraphQLRequestParser
{
    private string? ParseStringOrNull()
    {
        switch (_reader.Kind)
        {
            case TokenKind.String:
                {
                    var value = _reader.GetString();
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

    private IReadOnlyDictionary<string, object?>? ParseObjectOrNull()
    {
        switch (_reader.Kind)
        {
            case TokenKind.LeftBrace:
                return ParseObject();

            case TokenKind.Name when _reader.Value.SequenceEqual(GraphQLKeywords.Null):
                _reader.MoveNext();
                return null;

            default:
                throw ThrowHelper.ExpectedObjectOrNull(_reader);
        }
    }

    private IReadOnlyList<IReadOnlyDictionary<string, object?>>? ParseVariables()
    {
        switch (_reader.Kind)
        {
            case TokenKind.LeftBrace:
                return new[] { ParseVariablesObject(), };
            
            case TokenKind.LeftBracket:
                var list = new List<IReadOnlyDictionary<string, object?>>();
                _reader.Expect(TokenKind.LeftBracket);

                while (_reader.Kind != TokenKind.RightBracket)
                {
                    list.Add(ParseObject());
                }
                
                _reader.Expect(TokenKind.RightBracket);

                return list;

            case TokenKind.Name when _reader.Value.SequenceEqual(GraphQLKeywords.Null):
                _reader.MoveNext();
                return null;

            default:
                throw ThrowHelper.ExpectedObjectOrNull(_reader);
        }
    }
    
    private IReadOnlyDictionary<string, object?> ParseVariablesObject()
    {
        switch (_reader.Kind)
        {
            case TokenKind.LeftBrace:
                _reader.Expect(TokenKind.LeftBrace);

                var obj = new Dictionary<string, object?>();

                while (_reader.Kind != TokenKind.RightBrace)
                {
                    if (_reader.Kind != TokenKind.String)
                    {
                        throw new SyntaxException(
                            _reader,
                            ParseMany_InvalidOpenToken,
                            TokenKind.String,
                            TokenPrinter.Print(in _reader));
                    }

                    var name = _reader.GetString();
                    _reader.MoveNext();
                    _reader.Expect(TokenKind.Colon);
                    var value = ParseValueSyntax();
                    obj.Add(name, value);
                }

                // skip closing token
                _reader.Expect(TokenKind.RightBrace);

                return obj;

            default:
                throw ThrowHelper.ExpectedObjectOrNull(_reader);
        }
    }

    private IReadOnlyList<object?>? ParseBatchResponse()
    {
        switch (_reader.Kind)
        {
            case TokenKind.LeftBracket:
                var list = new List<object?>();
                _reader.Expect(TokenKind.LeftBracket);

                while (_reader.Kind != TokenKind.RightBracket)
                {
                    list.Add(ParseResponse());
                }

                return list;

            case TokenKind.Name when _reader.Value.SequenceEqual(GraphQLKeywords.Null):
                _reader.MoveNext();
                return null;

            default:
                throw ThrowHelper.ExpectedObjectOrNull(_reader);
        }
    }

    private IReadOnlyDictionary<string, object?>? ParseResponse()
    {
        switch (_reader.Kind)
        {
            case TokenKind.LeftBrace:
                _reader.Expect(TokenKind.LeftBrace);

                var obj = new Dictionary<string, object?>();

                while (_reader.Kind != TokenKind.RightBrace)
                {
                    if (_reader.Kind != TokenKind.String)
                    {
                        throw new SyntaxException(
                            _reader,
                            ParseMany_InvalidOpenToken,
                            TokenKind.String,
                            TokenPrinter.Print(in _reader));
                    }

                    var name = _reader.GetString();
                    _reader.MoveNext();
                    _reader.Expect(TokenKind.Colon);
                    var value = ParseResponseValue();
                    obj.Add(name, value);
                }

                // skip closing token
                _reader.Expect(TokenKind.RightBrace);

                return obj;

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

    public static bool TryExtractHash(
        IReadOnlyDictionary<string, object?>? extensions,
        IDocumentHashProvider? hashProvider,
        [NotNullWhen(true)] out string? hash)
    {
        if (extensions is not null
            && hashProvider is not null
            && extensions.TryGetValue(_persistedQuery, out var obj)
            && obj is IReadOnlyDictionary<string, object> persistedQuery
            && persistedQuery.TryGetValue(hashProvider.Name, out obj)
            && obj is string h)
        {
            hash = h;
            return true;
        }

        hash = null;
        return false;
    }
}
