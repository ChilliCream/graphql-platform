using System.Buffers;
using System.Text.Json;

namespace HotChocolate.Language;

public ref partial struct Utf8GraphQLRequestParser
{
    private const string PersistedQuery = "persistedQuery";
    private static readonly JsonReaderOptions s_jsonOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };
    private static readonly ArrayPool<GraphQLRequest> s_requestPool = ArrayPool<GraphQLRequest>.Shared;
    private static readonly ArrayPool<byte> s_bytePool = ArrayPool<byte>.Shared;

    private readonly ReadOnlySpan<byte> _requestData;
    private readonly IDocumentHashProvider? _hashProvider;
    private readonly IDocumentCache? _cache;
    private readonly bool _useCache;
    private readonly ParserOptions _options;

    public Utf8GraphQLRequestParser(
        ReadOnlySpan<byte> requestData,
        ParserOptions? options = null,
        IDocumentCache? cache = null,
        IDocumentHashProvider? hashProvider = null)
    {
        _requestData = requestData;
        _options = options ?? ParserOptions.Default;
        _cache = cache;
        _hashProvider = hashProvider;
        _useCache = cache is not null;
    }

    public readonly IReadOnlyList<GraphQLRequest> Parse()
    {
        try
        {
            var reader = new Utf8JsonReader(_requestData, s_jsonOptions);

            if (!reader.Read())
            {
                throw new ArgumentException("Empty JSON document.", nameof(_requestData));
            }

            return reader.TokenType switch
            {
                JsonTokenType.StartObject => [ParseRequest(ref reader)],
                JsonTokenType.StartArray => ParseBatchRequest(ref reader),
                _ => throw new InvalidOperationException("Invalid request structure. Expected object or array.")
            };
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid JSON document.", nameof(_requestData), ex);
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

                    rentedArray[count++] = ParseRequest(ref reader);
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

    private readonly GraphQLRequest ParseRequest(ref Utf8JsonReader reader)
    {
        DocumentNode? document = null;
        OperationDocumentId? documentId = null;
        OperationDocumentHash? documentHash = null;
        string? operationName = null;
        ErrorHandlingMode? errorHandlingMode = null;
        JsonDocument? variables = null;
        JsonDocument? extensions = null;
        var documentBody = default(ReadOnlySpan<byte>);
        var documentSequence = default(ReadOnlySequence<byte>);
        var containsDocument = false;
        var isDocumentEscaped = false;
        var hasDocumentSequence = false;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            if (reader.ValueTextEquals("query"u8))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.String)
                {
                    // Optimized path: check if we need unescaping or sequence handling
                    if (reader.ValueIsEscaped || reader.HasValueSequence)
                    {
                        // Rare case: needs unescaping or sequence consolidation
                        containsDocument = true;
                        isDocumentEscaped = reader.ValueIsEscaped;
                        hasDocumentSequence = reader.HasValueSequence;

                        if (reader.HasValueSequence)
                        {
                            documentSequence = reader.ValueSequence;
                        }
                        else
                        {
                            documentBody = reader.ValueSpan;
                        }
                    }
                    else if (reader.ValueSpan.Length > 0)
                    {
                        // Fast path: no escaping needed, use ValueSpan directly
                        containsDocument = true;
                        documentBody = reader.ValueSpan;
                    }
                }
            }
            else if (reader.ValueTextEquals("id"u8) || reader.ValueTextEquals("documentId"u8))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.String)
                {
                    var id = reader.GetString();
                    if (!string.IsNullOrEmpty(id))
                    {
                        documentId = new OperationDocumentId(id);
                    }
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
            }
            else if (reader.ValueTextEquals("variables"u8))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    variables = JsonDocument.ParseValue(ref reader);
                }
                else if (reader.TokenType == JsonTokenType.Null)
                {
                    variables = null;
                }
                else
                {
                    reader.Skip();
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
                    extensions = null;
                }
                else
                {
                    reader.Skip();
                }
            }
            else
            {
                reader.Read();
                reader.Skip();
            }
        }

        // Handle persisted queries via extensions
        if (!containsDocument
            && documentId is null
            && _useCache
            && extensions is not null
            && TryExtractHash(extensions, out var hash))
        {
            documentId = new OperationDocumentId(hash);
            documentHash = new OperationDocumentHash(hash, _hashProvider!.Name, _hashProvider.Format);
        }

        // Parse the GraphQL document if provided
        if (containsDocument)
        {
            ParseDocument(
                documentBody,
                documentSequence,
                isDocumentEscaped,
                hasDocumentSequence,
                documentId,
                ref document,
                ref documentHash,
                ref documentId);
        }

        // Validation
        if (document is null && documentId is null)
        {
            throw new InvalidOperationException("Request must contain either a query or a document id.");
        }

        return new GraphQLRequest(
            document,
            documentId,
            documentHash,
            operationName,
            errorHandlingMode,
            variables,
            extensions);
    }

    private readonly void ParseDocument(
        ReadOnlySpan<byte> documentBody,
        ReadOnlySequence<byte> documentSequence,
        bool isEscaped,
        bool hasSequence,
        OperationDocumentId? requestDocumentId,
        ref DocumentNode? document,
        ref OperationDocumentHash? documentHash,
        ref OperationDocumentId? documentId)
    {
        ReadOnlySpan<byte> queryBytes;
        byte[]? rentedBuffer = null;

        try
        {
            // Handle the 4 possible scenarios
            if (!isEscaped && !hasSequence)
            {
                // Scenario 1: No escapes, no sequence (COMMON - ~99%)
                // Zero allocations - use documentBody directly
                queryBytes = documentBody;
            }
            else if (isEscaped && !hasSequence)
            {
                // Scenario 2: Has escapes, no sequence (RARE)
                // Allocate buffer and unescape
                var maxLength = documentBody.Length;
                rentedBuffer = s_bytePool.Rent(maxLength);
                var unescapedSpan = rentedBuffer.AsSpan();
                Utf8Helper.Unescape(documentBody, ref unescapedSpan, isBlockString: false);
                queryBytes = unescapedSpan;
            }
            else if (!isEscaped && hasSequence)
            {
                // Scenario 3: No escapes, has sequence (VERY RARE)
                // Copy sequence to contiguous buffer
                var totalLength = (int)documentSequence.Length;
                rentedBuffer = s_bytePool.Rent(totalLength);
                documentSequence.CopyTo(rentedBuffer);
                queryBytes = rentedBuffer.AsSpan(0, totalLength);
            }
            else // isEscaped && hasSequence
            {
                // Scenario 4: Has escapes AND sequence (ULTRA RARE)
                // Copy sequence first, then unescape
                var totalLength = (int)documentSequence.Length;
                var tempBuffer = s_bytePool.Rent(totalLength);
                try
                {
                    documentSequence.CopyTo(tempBuffer);
                    var escapedSpan = tempBuffer.AsSpan(0, totalLength);

                    rentedBuffer = s_bytePool.Rent(totalLength);
                    var unescapedSpan = rentedBuffer.AsSpan();
                    Utf8Helper.Unescape(escapedSpan, ref unescapedSpan, isBlockString: false);
                    queryBytes = unescapedSpan;
                }
                finally
                {
                    s_bytePool.Return(tempBuffer);
                }
            }

            // Now use queryBytes for parsing and caching (same as before)
            if (_useCache)
            {
                if (requestDocumentId.HasValue
                    && _cache!.TryGetDocument(requestDocumentId.Value.Value, out var cachedDocument))
                {
                    document = cachedDocument.Body;
                    documentHash = cachedDocument.Hash;
                }
                else
                {
                    var hash = _hashProvider!.ComputeHash(queryBytes);
                    documentHash = hash;

                    if (_cache!.TryGetDocument(hash.Value, out cachedDocument))
                    {
                        document = cachedDocument.Body;
                    }
                    else
                    {
                        document = queryBytes.Length == 0
                            ? null
                            : Utf8GraphQLParser.Parse(queryBytes, _options);
                    }

                    if (!requestDocumentId.HasValue)
                    {
                        documentId = new OperationDocumentId(hash.Value);
                    }
                }
            }
            else
            {
                document = Utf8GraphQLParser.Parse(queryBytes, _options);
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

    private readonly bool TryExtractHash(JsonDocument extensions, out string hash)
    {
        if (extensions.RootElement.TryGetProperty(PersistedQuery, out var persistedQuery)
            && persistedQuery.ValueKind == JsonValueKind.Object
            && _hashProvider is not null
            && persistedQuery.TryGetProperty(_hashProvider.Name, out var hashElement)
            && hashElement.ValueKind == JsonValueKind.String)
        {
            hash = hashElement.GetString()!;
            return true;
        }

        hash = string.Empty;
        return false;
    }

    public static IReadOnlyList<GraphQLRequest> Parse(
        ReadOnlySpan<byte> requestData,
        ParserOptions? options = null,
        IDocumentCache? cache = null,
        IDocumentHashProvider? hashProvider = null)
        => new Utf8GraphQLRequestParser(requestData, options, cache, hashProvider).Parse();
}
