// ReSharper disable RedundantSuppressNullableWarningExpression

using System.Buffers;
using Microsoft.AspNetCore.Http;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.AspNetCore.ThrowHelper;
using static HotChocolate.Language.Utf8GraphQLRequestParser;

namespace HotChocolate.AspNetCore.Serialization;

internal sealed class DefaultHttpRequestParser : IHttpRequestParser
{
    private const int MinRequestSize = 256;
    internal const string QueryIdKey = "id";
    private const string OperationNameKey = "operationName";
    internal const string QueryKey = "query";
    private const string VariablesKey = "variables";
    internal const string ExtensionsKey = "extensions";

    private readonly IDocumentCache _documentCache;
    private readonly IDocumentHashProvider _documentHashProvider;
    private readonly ParserOptions _parserOptions;
    private readonly int _maxRequestSize;

    public DefaultHttpRequestParser(
        IDocumentCache documentCache,
        IDocumentHashProvider documentHashProvider,
        int maxRequestSize,
        ParserOptions parserOptions)
    {
        ArgumentNullException.ThrowIfNull(documentCache);
        ArgumentNullException.ThrowIfNull(documentHashProvider);
        ArgumentNullException.ThrowIfNull(parserOptions);

        _documentCache = documentCache;
        _documentHashProvider = documentHashProvider;
        _maxRequestSize = maxRequestSize < MinRequestSize ? MinRequestSize : maxRequestSize;
        _parserOptions = parserOptions;
    }

    public ValueTask<IReadOnlyList<GraphQLRequest>> ParseRequestAsync(
        Stream requestBody,
        CancellationToken cancellationToken)
        => ReadAsync(requestBody, cancellationToken);

    public async ValueTask<GraphQLRequest> ParsePersistedOperationRequestAsync(
        string documentId,
        string? operationName,
        Stream requestBody,
        CancellationToken cancellationToken)
    {
        EnsureValidDocumentId(documentId);

        try
        {
            GraphQLRequest Parse(byte[] buffer, int length)
                => ParsePersistedOperationRequest(buffer, length, documentId, operationName);

            return await BufferHelper.ReadAsync(
                requestBody,
                Parse,
                _maxRequestSize,
                static (buffer, bytesBuffered, p) =>
                {
                    if (bytesBuffered == 0)
                    {
                        throw DefaultHttpRequestParser_RequestIsEmpty();
                    }

                    return p(buffer, bytesBuffered);
                },
                static () => throw DefaultHttpRequestParser_MaxRequestSizeExceeded(),
                cancellationToken);
        }
        catch (GraphQLRequestException)
        {
            throw;
        }
        catch (SyntaxException ex)
        {
            throw DefaultHttpRequestParser_SyntaxError(ex);
        }
        catch (Exception ex)
        {
            throw DefaultHttpRequestParser_UnexpectedError(ex);
        }
    }

    public GraphQLRequest ParseRequestFromParams(IQueryCollection parameters)
    {
        // next, we deserialize the GET request with the query request builder ...
        string? query = parameters[QueryKey];
        string? queryId = parameters[QueryIdKey];
        string? operationName = parameters[OperationNameKey];
        IReadOnlyDictionary<string, object?>? extensions = null;

        // if we have no query or query id, we cannot execute anything.
        if (string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(queryId))
        {
            // so, if we do not find a top-level query or top-level id, we will try to parse
            // the extensions and look in the extensions for Apollo's active persisted
            // query extensions.
            if ((string?)parameters[ExtensionsKey] is { Length: > 0 } se)
            {
                extensions = ParseJsonObject(se);
            }

            // we will use the request parser utils to extract the hash from the extensions.
            if (!TryExtractHash(extensions, _documentHashProvider, out var hash))
            {
                // if we cannot find any query hash in the extensions, or if the extensions are
                // null, we are unable to execute and will throw a request error.
                throw DefaultHttpRequestParser_QueryAndIdMissing();
            }

            // if we however found a query hash, we will use it as a query id and move on
            // to execute the query.
            queryId = hash;
        }

        if (!string.IsNullOrWhiteSpace(queryId))
        {
            EnsureValidDocumentId(queryId);
        }

        try
        {
            OperationDocumentHash? documentHash = null;
            DocumentNode? document = null;

            if (query?.Length > 0)
            {
                var result = ParseQueryString(query);
                documentHash = result.DocumentHash;
                document = result.Document;
            }

            IReadOnlyList<IReadOnlyDictionary<string, object?>>? variableSet = null;
            if ((string?)parameters[VariablesKey] is { Length: > 0 } sv)
            {
                variableSet = ParseVariables(sv);
            }

            if (extensions is null &&
                (string?)parameters[ExtensionsKey] is { Length: > 0 } se)
            {
                extensions = ParseJsonObject(se);
            }

            return new GraphQLRequest(
                document,
                queryId,
                documentHash,
                operationName,
                variableSet,
                extensions);
        }
        catch (SyntaxException ex)
        {
            throw DefaultHttpRequestParser_SyntaxError(ex);
        }
        catch (Exception ex)
        {
            throw DefaultHttpRequestParser_UnexpectedError(ex);
        }
    }

    public GraphQLRequest ParsePersistedOperationRequestFromParams(
        string operationId,
        string? operationName,
        IQueryCollection parameters)
    {
        operationName ??= parameters[OperationNameKey];
        EnsureValidDocumentId(operationId);

        try
        {
            IReadOnlyList<IReadOnlyDictionary<string, object?>>? variableSet = null;
            if ((string?)parameters[VariablesKey] is { Length: > 0 } sv)
            {
                variableSet = ParseVariables(sv);
            }

            IReadOnlyDictionary<string, object?>? extensions = null;
            if (extensions is null &&
                (string?)parameters[ExtensionsKey] is { Length: > 0 } se)
            {
                extensions = ParseJsonObject(se);
            }

            return new GraphQLRequest(
                null,
                operationId,
                null,
                operationName,
                variableSet,
                extensions);
        }
        catch (SyntaxException ex)
        {
            throw DefaultHttpRequestParser_SyntaxError(ex);
        }
        catch (Exception ex)
        {
            throw DefaultHttpRequestParser_UnexpectedError(ex);
        }
    }

    private (OperationDocumentHash DocumentHash, DocumentNode Document) ParseQueryString(string sourceText)
    {
        var length = checked(sourceText.Length * 4);
        byte[]? source = null;

        var sourceSpan = length <= GraphQLConstants.StackallocThreshold
            ? stackalloc byte[length]
            : source = ArrayPool<byte>.Shared.Rent(length);

        Utf8GraphQLParser.ConvertToBytes(sourceText, ref sourceSpan);
        var document = Utf8GraphQLParser.Parse(sourceSpan, _parserOptions);
        var documentHash = _documentHashProvider.ComputeHash(sourceSpan);

        if (source != null)
        {
            sourceSpan.Clear();
            ArrayPool<byte>.Shared.Return(source);
        }

        return (documentHash, document);
    }

    public IReadOnlyList<GraphQLRequest> ParseRequest(
        string sourceText)
    {
        try
        {
            return Parse(sourceText, _parserOptions, _documentCache, _documentHashProvider);
        }
        catch (OperationIdFormatException)
        {
            throw ErrorHelper.InvalidOperationIdFormat();
        }
    }

    private async ValueTask<IReadOnlyList<GraphQLRequest>> ReadAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        try
        {
            Func<byte[], int, IReadOnlyList<GraphQLRequest>> parse = ParseRequest;

            return await BufferHelper.ReadAsync(
                stream,
                parse,
                _maxRequestSize,
                static (buffer, bytesBuffered, p) =>
                {
                    if (bytesBuffered == 0)
                    {
                        throw DefaultHttpRequestParser_RequestIsEmpty();
                    }

                    return p(buffer, bytesBuffered);
                },
                static () => throw DefaultHttpRequestParser_MaxRequestSizeExceeded(),
                cancellationToken);
        }
        catch (GraphQLRequestException)
        {
            throw;
        }
        catch (SyntaxException ex)
        {
            throw DefaultHttpRequestParser_SyntaxError(ex);
        }
        catch (Exception ex)
        {
            throw DefaultHttpRequestParser_UnexpectedError(ex);
        }
    }

    private IReadOnlyList<GraphQLRequest> ParseRequest(
        byte[] buffer,
        int bytesBuffered)
    {
        var graphQLData = new ReadOnlySpan<byte>(buffer);
        graphQLData = graphQLData[..bytesBuffered];

        var requestParser = new Utf8GraphQLRequestParser(
            graphQLData,
            _parserOptions,
            _documentCache,
            _documentHashProvider);

        return requestParser.Parse();
    }

    private GraphQLRequest ParsePersistedOperationRequest(
        byte[] buffer,
        int bytesBuffered,
        string documentId,
        string? operationName)
    {
        var graphQLData = new ReadOnlySpan<byte>(buffer);
        graphQLData = graphQLData[..bytesBuffered];

        var requestParser = new Utf8GraphQLRequestParser(
            graphQLData,
            _parserOptions,
            _documentCache,
            _documentHashProvider);

        return requestParser.ParsePersistedOperation(documentId, operationName);
    }

    private static void EnsureValidDocumentId(string documentId)
    {
        if (!OperationDocumentId.IsValidId(documentId))
        {
            throw ErrorHelper.InvalidOperationIdFormat();
        }
    }
}
