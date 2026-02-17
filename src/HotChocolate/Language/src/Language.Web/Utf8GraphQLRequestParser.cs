using System.Buffers;
using System.Text.Json;
using static HotChocolate.Language.Properties.LangWebResources;

namespace HotChocolate.Language;

public ref struct Utf8GraphQLRequestParser
{
    private const string PersistedQuery = "persistedQuery";
    private static readonly JsonReaderOptions s_jsonOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };
    private static readonly ArrayPool<GraphQLRequest> s_requestPool = ArrayPool<GraphQLRequest>.Shared;
    private static readonly ArrayPool<byte> s_bytePool = ArrayPool<byte>.Shared;

    private readonly IDocumentHashProvider? _hashProvider;
    private readonly IDocumentCache? _cache;
    private readonly bool _useCache;
    private readonly ParserOptions _options;

    public Utf8GraphQLRequestParser(
        ParserOptions? options = null,
        IDocumentCache? cache = null,
        IDocumentHashProvider? hashProvider = null)
    {
        _options = options ?? ParserOptions.Default;
        _cache = cache;
        _hashProvider = hashProvider;
        _useCache = cache is not null;
    }

    public readonly GraphQLRequest[] Parse(ReadOnlySpan<byte> requestData)
    {
        var reader = new Utf8JsonReader(requestData, s_jsonOptions);
        return Parse(ref reader);
    }

    public readonly GraphQLRequest[] Parse(ReadOnlySequence<byte> requestData)
    {
        var reader = new Utf8JsonReader(requestData, s_jsonOptions);
        return Parse(ref reader);
    }

    private readonly GraphQLRequest[] Parse(ref Utf8JsonReader reader)
    {
        try
        {
            if (!reader.Read())
            {
                throw new InvalidGraphQLRequestException(
                    Utf8GraphQLRequestParser_Parse_EmptyJSONDocument);
            }

            return reader.TokenType switch
            {
                JsonTokenType.StartObject => [ParseRequest(ref reader, OperationDocumentId.Empty)],
                JsonTokenType.StartArray => ParseBatchRequest(ref reader),
                _ => throw new InvalidGraphQLRequestException(Utf8GraphQLRequestParser_Parse_InvalidRequestStructure)
            };
        }
        catch (JsonException ex)
        {
            throw new InvalidGraphQLRequestException(
                Utf8GraphQLRequestParser_Parse_InvalidJSONDocument_,
                ex);
        }
    }

    public readonly GraphQLRequest ParsePersistedOperation(
        OperationDocumentId operationId,
        string? operationName,
        ReadOnlySpan<byte> requestData)
    {
        var reader = new Utf8JsonReader(requestData, s_jsonOptions);
        return ParsePersistedOperation(operationId, operationName, ref reader);
    }

    public readonly GraphQLRequest ParsePersistedOperation(
        OperationDocumentId operationId,
        string? operationName,
        ReadOnlySequence<byte> requestData)
    {
        var reader = new Utf8JsonReader(requestData, s_jsonOptions);
        return ParsePersistedOperation(operationId, operationName, ref reader);
    }

    private readonly GraphQLRequest ParsePersistedOperation(
        OperationDocumentId operationId,
        string? operationName,
        ref Utf8JsonReader reader)
    {
        try
        {
            if (!reader.Read())
            {
                throw new InvalidGraphQLRequestException(
                    Utf8GraphQLRequestParser_Parse_EmptyJSONDocument);
            }

            var request = ParseRequest(ref reader, operationId);

            return new GraphQLRequest(
                request.Document,
                operationId,
                request.DocumentHash,
                operationName,
                request.ErrorHandlingMode,
                request.Variables,
                request.Extensions);
        }
        catch (JsonException ex)
        {
            throw new InvalidGraphQLRequestException(
                Utf8GraphQLRequestParser_Parse_InvalidJSONDocument_,
                ex);
        }
    }

    private readonly GraphQLRequest[] ParseBatchRequest(ref Utf8JsonReader reader)
    {
        const int initialCapacity = 16;
        var rentedArray = s_requestPool.Rent(initialCapacity);
        var count = 0;

        try
        {
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    // Expand array if needed
                    if (count == rentedArray.Length)
                    {
                        var newArray = s_requestPool.Rent(rentedArray.Length * 2);
                        Array.Copy(rentedArray, newArray, count);
                        s_requestPool.Return(rentedArray, clearArray: true);
                        rentedArray = newArray;
                    }

                    rentedArray[count++] = ParseRequest(ref reader, OperationDocumentId.Empty);
                }
            }

            if (count == 0)
            {
                return [];
            }

            var result = new GraphQLRequest[count];
            Array.Copy(rentedArray, result, count);
            return result;
        }
        finally
        {
            s_requestPool.Return(rentedArray, clearArray: true);
        }
    }

    private readonly GraphQLRequest ParseRequest(ref Utf8JsonReader reader, OperationDocumentId documentId = default)
    {
        DocumentNode? document = null;
        OperationDocumentHash documentHash = default;
        string? operationName = null;
        ErrorHandlingMode? errorHandlingMode = null;
        JsonDocument? variables = null;
        JsonDocument? extensions = null;
        ReadOnlySpan<byte> documentBody = default;
        var isDocumentEscaped = false;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType is not JsonTokenType.PropertyName)
            {
                throw new InvalidGraphQLRequestException(
                    $"Unexpected token type {reader.TokenType}. Expected PropertyName.");
            }

            if (reader.ValueTextEquals("query"u8))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.String)
                {
                    isDocumentEscaped = reader.ValueIsEscaped;
                    documentBody = reader.ValueSpan;
                }
                else if (reader.TokenType == JsonTokenType.Null)
                {
                    // Null is acceptable, just skip it
                }
                else
                {
                    throw ThrowHelper.InvalidQueryValue(reader.TokenType);
                }
            }
            else if (reader.ValueTextEquals("id"u8) || reader.ValueTextEquals("documentId"u8))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.String)
                {
                    var id = reader.GetString();
                    if (!string.IsNullOrEmpty(id) && !OperationDocumentId.TryParse(id, out documentId))
                    {
                        throw ThrowHelper.InvalidDocumentIdFormat();
                    }
                }
                else if (reader.TokenType == JsonTokenType.Null)
                {
                    // Null is acceptable, just skip it
                }
                else
                {
                    throw ThrowHelper.InvalidDocumentIdValue(reader.TokenType);
                }
            }
            else if (reader.ValueTextEquals("operationName"u8))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.String)
                {
                    var name = reader.GetString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        operationName = name;
                    }
                }
                else if (reader.TokenType == JsonTokenType.Null)
                {
                    // Null is acceptable, just skip it
                }
                else
                {
                    throw ThrowHelper.InvalidOperationNameValue(reader.TokenType);
                }
            }
            else if (reader.ValueTextEquals("onError"u8))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.String)
                {
                    var mode = reader.GetString();
                    errorHandlingMode = mode?.ToUpperInvariant() switch
                    {
                        "PROPAGATE" => ErrorHandlingMode.Propagate,
                        "NULL" => ErrorHandlingMode.Null,
                        "HALT" => ErrorHandlingMode.Halt,
                        _ => null
                    };
                }
                else if (reader.TokenType == JsonTokenType.Null)
                {
                    // Null is acceptable, just skip it
                }
                else
                {
                    throw ThrowHelper.InvalidOnErrorValue(reader.TokenType);
                }
            }
            else if (reader.ValueTextEquals("variables"u8))
            {
                reader.Read();
                if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    variables = JsonDocument.ParseValue(ref reader);
                }
                else if (reader.TokenType == JsonTokenType.Null)
                {
                    // Null is acceptable, just skip it
                }
                else
                {
                    throw ThrowHelper.InvalidVariablesValue(reader.TokenType);
                }
            }
            else if (reader.ValueTextEquals("extensions"u8))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    extensions = JsonDocument.ParseValue(ref reader);
                }
                else if (reader.TokenType == JsonTokenType.Null)
                {
                    // Null is acceptable, just skip it
                }
                else
                {
                    throw ThrowHelper.InvalidExtensionsValue(reader.TokenType);
                }
            }
            else if (reader.ValueTextEquals("operationType"u8))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.String)
                {
                    // A String is acceptable, just skip it
                }
                else if (reader.TokenType == JsonTokenType.Null)
                {
                    // Null is acceptable, just skip it
                }
                else
                {
                    throw ThrowHelper.InvalidOperationTypeValue(reader.TokenType);
                }
            }
            else
            {
                throw ThrowHelper.UnknownRequestProperty(reader.ValueSpan);
            }
        }

        // Handle persisted queries via extensions
        if (documentBody.IsEmpty
            && documentId.IsEmpty
            && _useCache
            && extensions is not null
            && TryExtractHash(extensions, out var hash))
        {
            documentId = new OperationDocumentId(hash);
            documentHash = new OperationDocumentHash(hash, _hashProvider!.Name, _hashProvider.Format);
        }

        // Parse the GraphQL document if provided
        if (!documentBody.IsEmpty)
        {
            ParseDocument(
                documentBody,
                isDocumentEscaped,
                ref document,
                ref documentHash,
                ref documentId);
        }

        // Validation
        if (document is null && documentId.IsEmpty)
        {
            throw new InvalidGraphQLRequestException("Request must contain either a query or a document id.");
        }

        return new GraphQLRequest(
            document,
            documentId.IsEmpty ? null : documentId,
            documentHash.IsEmpty ? null : documentHash,
            operationName,
            errorHandlingMode,
            variables,
            extensions);
    }

    private readonly void ParseDocument(
        ReadOnlySpan<byte> documentBody,
        bool isEscaped,
        ref DocumentNode? document,
        ref OperationDocumentHash documentHash,
        ref OperationDocumentId documentId)
    {
        byte[]? rentedBuffer = null;

        try
        {
            if (isEscaped)
            {
                var requiredBufferLength = documentBody.Length;
                rentedBuffer = s_bytePool.Rent(requiredBufferLength);
                Span<byte> unescapedDocumentBody = rentedBuffer;
                Utf8Helper.Unescape(documentBody, ref unescapedDocumentBody, isBlockString: false);
                documentBody = unescapedDocumentBody;
            }

            // Now use the document bytes for parsing and caching
            if (_useCache)
            {
                if (documentId.HasValue && _cache!.TryGetDocument(documentId.Value, out var cachedDocument))
                {
                    document = cachedDocument.Body;
                    documentHash = cachedDocument.Hash;
                }
                else if (documentBody.Length > 0)
                {
                    var hash = _hashProvider!.ComputeHash(documentBody);
                    documentHash = hash;

                    document = _cache!.TryGetDocument(hash.Value, out cachedDocument)
                        ? cachedDocument.Body
                        : Utf8GraphQLParser.Parse(documentBody, _options);

                    if (documentId.IsEmpty)
                    {
                        documentId = new OperationDocumentId(hash.Value);
                    }
                }
            }
            else
            {
                document = Utf8GraphQLParser.Parse(documentBody, _options);
            }
        }
        finally
        {
            if (rentedBuffer != null)
            {
                s_bytePool.Return(rentedBuffer);
            }
        }
    }

    private readonly bool TryExtractHash(JsonDocument? extensions, out string hash)
        => TryExtractHashInternal(extensions, _hashProvider, out hash);

    public static bool TryExtractHash(
        JsonDocument? extensions,
        IDocumentHashProvider documentHashProvider,
        out string hash)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(documentHashProvider);
#else
        if (documentHashProvider is null)
        {
            throw new ArgumentNullException(nameof(documentHashProvider));
        }
#endif

        return TryExtractHashInternal(extensions, documentHashProvider, out hash);
    }

    private static bool TryExtractHashInternal(
        JsonDocument? extensions,
        IDocumentHashProvider? documentHashProvider,
        out string hash)
    {
        if (extensions is not null
            && extensions.RootElement.TryGetProperty(PersistedQuery, out var persistedQuery)
            && persistedQuery.ValueKind == JsonValueKind.Object
            && documentHashProvider is not null
            && persistedQuery.TryGetProperty(documentHashProvider.Name, out var hashElement)
            && hashElement.ValueKind == JsonValueKind.String)
        {
            hash = hashElement.GetString()!;
            return true;
        }

        hash = string.Empty;
        return false;
    }

    public static GraphQLRequest[] Parse(
        ReadOnlySpan<byte> requestData,
        ParserOptions? options = null,
        IDocumentCache? cache = null,
        IDocumentHashProvider? hashProvider = null)
        => new Utf8GraphQLRequestParser(options, cache, hashProvider).Parse(requestData);

    public static GraphQLRequest[] Parse(
        ReadOnlySequence<byte> requestData,
        ParserOptions? options = null,
        IDocumentCache? cache = null,
        IDocumentHashProvider? hashProvider = null)
        => new Utf8GraphQLRequestParser(options, cache, hashProvider).Parse(requestData);
}
