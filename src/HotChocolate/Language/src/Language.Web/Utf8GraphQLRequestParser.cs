using System.Buffers;
using static HotChocolate.Language.Properties.LangWebResources;

namespace HotChocolate.Language;

public ref partial struct Utf8GraphQLRequestParser
{
    private const string PersistedQuery = "persistedQuery";
    private readonly IDocumentHashProvider? _hashProvider;
    private readonly IDocumentCache? _cache;
    private readonly bool _useCache;
    private readonly ParserOptions _options;
    private Utf8GraphQLReader _reader;

    public Utf8GraphQLRequestParser(
        ReadOnlySpan<byte> requestData,
        ParserOptions? options = null,
        IDocumentCache? cache = null,
        IDocumentHashProvider? hashProvider = null)
    {
        _reader = new Utf8GraphQLReader(requestData);
        _options = options ?? ParserOptions.Default;
        _cache = cache;
        _hashProvider = hashProvider;
        _useCache = cache is not null;
    }

    public GraphQLRequest ParsePersistedOperation(string operationId, string? operationName)
    {
        _reader.MoveNext();

        if (_reader.Kind == TokenKind.LeftBrace)
        {
            var request = ParseMutableRequest(operationId);

            return new GraphQLRequest
            (
                null,
                request.DocumentId,
                null,
                operationName ?? request.OperationName,
                request.Variables,
                request.Extensions
            );
        }

        throw ThrowHelper.InvalidRequestStructure(_reader);
    }

    public IReadOnlyList<GraphQLRequest> Parse()
    {
        _reader.MoveNext();

        if (_reader.Kind == TokenKind.LeftBrace)
        {
            var singleRequest = ParseRequest();
            return [singleRequest];
        }

        if (_reader.Kind == TokenKind.LeftBracket)
        {
            return ParseBatchRequest();
        }

        throw ThrowHelper.InvalidRequestStructure(_reader);
    }

    public GraphQLSocketMessage ParseMessage()
    {
        _reader.MoveNext();
        _reader.Expect(TokenKind.LeftBrace);

        var message = new Message();

        while (_reader.Kind != TokenKind.RightBrace)
        {
            ParseMessageProperty(ref message);
        }

        if (message.Type is null)
        {
            throw new InvalidOperationException(
                "The GraphQL socket message had no type property specified.");
        }

        return new GraphQLSocketMessage
        (
            message.Type,
            message.Id,
            message.Payload
        );
    }

    public object? ParseJson()
    {
        _reader.MoveNext();
        return ParseValue();
    }

    private IReadOnlyList<GraphQLRequest> ParseBatchRequest()
    {
        var batch = new List<GraphQLRequest>();

        _reader.Expect(TokenKind.LeftBracket);

        while (_reader.Kind != TokenKind.RightBracket)
        {
            batch.Add(ParseRequest());
            _reader.MoveNext();
        }

        return batch;
    }

    private GraphQLRequest ParseRequest()
    {
        var request = ParseMutableRequest();

        return new GraphQLRequest
        (
            request.Document,
            request.DocumentId,
            request.DocumentHash,
            request.OperationName,
            request.Variables,
            request.Extensions
        );
    }

    private Request ParseMutableRequest(string? documentId = null)
    {
        var request = new Request();

        _reader.Expect(TokenKind.LeftBrace);

        while (_reader.Kind != TokenKind.RightBrace)
        {
            ParseRequestProperty(ref request);
        }

        if (documentId is not null)
        {
            request.DocumentId = documentId;
        }

        if (!request.ContainsDocument && request.DocumentId is null)
        {
            if (_useCache && TryExtractHash(request.Extensions, _hashProvider, out var hash))
            {
                request.DocumentId = hash;
                request.DocumentHash = new OperationDocumentHash(hash, _hashProvider!.Name, _hashProvider.Format);
            }
            else
            {
                throw ThrowHelper.NoIdAndNoQuery(_reader);
            }
        }

        if (request.ContainsDocument)
        {
            ParseDocument(ref request);
        }

        if (request.Document is null && request.DocumentId is null)
        {
            throw ThrowHelper.NoIdAndNoQuery(_reader);
        }

        return request;
    }

    private void ParseRequestProperty(ref Request request)
    {
        var fieldName = _reader.Expect(TokenKind.String);
        _reader.Expect(TokenKind.Colon);

        switch (fieldName[0])
        {
            case I:
                if (fieldName.SequenceEqual(IdProperty))
                {
                    request.DocumentId = ParseOperationId(_reader);
                }
                break;

            case D:
                if (fieldName.SequenceEqual(DocumentIdProperty))
                {
                    request.DocumentId = ParseOperationId(_reader);
                }
                break;

            case Q:
                if (fieldName.SequenceEqual(QueryProperty))
                {
                    var isNullOrEmpty = IsNullToken() || _reader.Value.Length == 0;
                    request.ContainsDocument = !isNullOrEmpty;

                    if (request.ContainsDocument && _reader.Kind != TokenKind.String)
                    {
                        throw ThrowHelper.QueryMustBeStringOrNull(_reader);
                    }

                    request.DocumentBody = _reader.Value;
                    _reader.MoveNext();
                }
                break;

            case O:
                if (fieldName.SequenceEqual(OperationNameProperty))
                {
                    request.OperationName = ParseStringOrNull();
                }
                break;

            case V:
                if (fieldName.SequenceEqual(VariablesProperty))
                {
                    request.Variables = ParseVariables();
                }
                break;

            case E:
                if (fieldName.SequenceEqual(ExtensionsProperty))
                {
                    request.Extensions = ParseObjectOrNull();
                }
                break;

            default:
                SkipValue();
                break;
        }
    }

    private void ParseMessageProperty(ref Message message)
    {
        var fieldName = _reader.Expect(TokenKind.String);
        _reader.Expect(TokenKind.Colon);

        switch (fieldName[0])
        {
            case T:
                if (fieldName.SequenceEqual(TypeProperty))
                {
                    message.Type = ParseStringOrNull();
                }
                break;

            case I:
                if (fieldName.SequenceEqual(IdProperty))
                {
                    message.Id = ParseStringOrNull();
                }
                break;

            case P:
                if (fieldName.SequenceEqual(PayloadProperty))
                {
                    var start = _reader.Start;
                    var hasPayload = !IsNullToken();
                    var end = SkipValue();
                    message.Payload = hasPayload
                        ? _reader.GraphQLData.Slice(start, end - start)
                        : default;
                }
                break;

            default:
                throw ThrowHelper.UnexpectedProperty(_reader, fieldName);
        }
    }

    private void ParseDocument(ref Request request)
    {
        var length = request.DocumentBody.Length;

        byte[]? unescapedArray = null;

        var unescapedSpan = length <= GraphQLConstants.StackallocThreshold
            ? stackalloc byte[length]
            : (unescapedArray = ArrayPool<byte>.Shared.Rent(length));

        try
        {
            Utf8Helper.Unescape(request.DocumentBody, ref unescapedSpan, false);
            DocumentNode? document;

            if (_useCache)
            {
                if (request.DocumentId.HasValue
                    && _cache!.TryGetDocument(request.DocumentId.Value.Value, out var cachedDocument))
                {
                    document = cachedDocument.Body;
                    request.DocumentHash = cachedDocument.Hash;
                }
                else
                {
                    var hash = _hashProvider!.ComputeHash(unescapedSpan);
                    request.DocumentHash = hash;
                    if (_cache!.TryGetDocument(hash.Value, out cachedDocument))
                    {
                        document = cachedDocument.Body;
                    }
                    else
                    {
                        document = unescapedSpan.Length == 0 ? null : Utf8GraphQLParser.Parse(unescapedSpan, _options);
                    }

                    if (!request.DocumentId.HasValue)
                    {
                        request.DocumentId = hash.Value;
                    }
                }
            }
            else
            {
                document = Utf8GraphQLParser.Parse(unescapedSpan, _options);
            }

            if (document is not null)
            {
                request.Document = document;
            }
        }
        finally
        {
            if (unescapedArray != null)
            {
                unescapedSpan.Clear();
                ArrayPool<byte>.Shared.Return(unescapedArray);
            }
        }
    }

    public static IReadOnlyList<GraphQLRequest> Parse(
        ReadOnlySpan<byte> requestData,
        ParserOptions? options = null,
        IDocumentCache? cache = null,
        IDocumentHashProvider? hashProvider = null) =>
        new Utf8GraphQLRequestParser(requestData, options, cache, hashProvider).Parse();

    public static unsafe IReadOnlyList<GraphQLRequest> Parse(
        string sourceText,
        ParserOptions? options = null,
        IDocumentCache? cache = null,
        IDocumentHashProvider? hashProvider = null)
    {
        if (string.IsNullOrEmpty(sourceText))
        {
            throw new ArgumentException(SourceText_Empty, nameof(sourceText));
        }

        var length = checked(sourceText.Length * 4);
        byte[]? source = null;

        var sourceSpan = length <= GraphQLConstants.StackallocThreshold
            ? stackalloc byte[length]
            : source = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            Utf8GraphQLParser.ConvertToBytes(sourceText, ref sourceSpan);
            var parser = new Utf8GraphQLRequestParser(sourceSpan, options, cache, hashProvider);
            return parser.Parse();
        }
        finally
        {
            if (source != null)
            {
                sourceSpan.Clear();
                ArrayPool<byte>.Shared.Return(source);
            }
        }
    }

    public static GraphQLSocketMessage ParseMessage(ReadOnlySpan<byte> messageData)
        => new Utf8GraphQLRequestParser(messageData).ParseMessage();
}
