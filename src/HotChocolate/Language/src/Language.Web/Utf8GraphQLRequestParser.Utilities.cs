using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using static HotChocolate.Language.Properties.LangWebResources;

namespace HotChocolate.Language;

public ref partial struct Utf8GraphQLRequestParser
{
    private OperationDocumentId? ParseOperationId(Utf8GraphQLReader reader)
    {
        switch (_reader.Kind)
        {
            case TokenKind.String:
                if (_reader.Value.Length == 0)
                {
                    _reader.MoveNext();
                    return null;
                }

                byte[]? rawStringBuffer = null;
                var length = _reader.Value.Length;

                var rawString = length <= GraphQLConstants.StackallocThreshold
                    ? stackalloc byte[length]
                    : rawStringBuffer = ArrayPool<byte>.Shared.Rent(length);

                try
                {
                    if (!Utf8GraphQLReader.TryGetRawString(_reader.Value, false, rawString, out var written))
                    {
                        throw new OperationIdFormatException(_reader);
                    }

                    if (written == 0)
                    {
                        _reader.MoveNext();
                        return null;
                    }

                    rawString = rawString.Slice(0, written);

                    if (!OperationDocumentId.IsValidId(rawString))
                    {
                        throw new OperationIdFormatException(_reader);
                    }

                    _reader.MoveNext();
                    return new OperationDocumentId(Utf8GraphQLReader.GetString(rawString));
                }
                finally
                {
                    if (rawStringBuffer != null)
                    {
                        rawString.Clear();
                        ArrayPool<byte>.Shared.Return(rawStringBuffer);
                    }
                }

            case TokenKind.Name when _reader.Value.SequenceEqual(GraphQLKeywords.Null):
                _reader.MoveNext();
                return null;

            default:
                throw ThrowHelper.ExpectedStringOrNull(_reader);
        }
    }

    private string? ParseStringOrNull()
    {
        switch (_reader.Kind)
        {
            case TokenKind.String:
                if (_reader.Value.Length == 0)
                {
                    _reader.MoveNext();
                    return null;
                }

                var value = _reader.GetString();
                _reader.MoveNext();
                return value;

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
                return new[] { ParseVariablesObject() };

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
                            TokenPrinter.Print(ref _reader));
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
                            TokenPrinter.Print(ref _reader));
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
            && extensions.TryGetValue(PersistedQuery, out var obj)
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
