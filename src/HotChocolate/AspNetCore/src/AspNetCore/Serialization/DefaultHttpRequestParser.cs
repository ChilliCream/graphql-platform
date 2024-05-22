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
    private const int _minRequestSize = 256;
    internal const string QueryIdKey = "id";
    private const string _operationNameKey = "operationName";
    internal const string QueryKey = "query";
    private const string _variablesKey = "variables";
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
        _documentCache = documentCache ??
            throw new ArgumentNullException(nameof(documentCache));
        _documentHashProvider = documentHashProvider ??
            throw new ArgumentNullException(nameof(documentHashProvider));
        _maxRequestSize = maxRequestSize < _minRequestSize
            ? _minRequestSize
            : maxRequestSize;
        _parserOptions = parserOptions ??
            throw new ArgumentNullException(nameof(parserOptions));
    }

    public ValueTask<IReadOnlyList<GraphQLRequest>> ParseRequestAsync(
        Stream requestBody,
        CancellationToken cancellationToken) 
        => ReadAsync(requestBody, cancellationToken);

    public async ValueTask<GraphQLRequest> ParsePersistedOperationRequestAsync(
        string operationId,
        Stream requestBody,
        CancellationToken cancellationToken)
    {
        EnsureValidQueryId(operationId);
        
        try
        {
            GraphQLRequest Parse(byte[] buffer, int length)
                => ParsePersistedOperationRequest(buffer, length, operationId);

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
        // next we deserialize the GET request with the query request builder ...
        string? query = parameters[QueryKey];
        string? queryId = parameters[QueryIdKey];
        string? operationName = parameters[_operationNameKey];
        IReadOnlyDictionary<string, object?>? extensions = null;

        // if we have no query or query id we cannot execute anything.
        if (string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(queryId))
        {
            // so, if we do not find a top-level query or top-level id we will try to parse
            // the extensions and look in the extensions for Apollo`s active persisted
            // query extensions.
            if ((string?)parameters[ExtensionsKey] is { Length: > 0, } se)
            {
                extensions = ParseJsonObject(se);
            }

            // we will use the request parser utils to extract the has from the extensions.
            if (!TryExtractHash(extensions, _documentHashProvider, out var hash))
            {
                // if we cannot find any query hash in the extensions or if the extensions are
                // null we are unable to execute and will throw a request error.
                throw DefaultHttpRequestParser_QueryAndIdMissing();
            }

            // if we however found a query hash we will use it as a query id and move on
            // to execute the query.
            queryId = hash;
        }

        if (!string.IsNullOrWhiteSpace(queryId))
        {
            EnsureValidQueryId(queryId);
        }

        try
        {
            string? queryHash = null;
            DocumentNode? document = null;

            if (query is { Length: > 0, })
            {
                var result = ParseQueryString(query);
                queryHash = result.QueryHash;
                document = result.Document;
            }

            IReadOnlyList<IReadOnlyDictionary<string, object?>>? variableSet = null;
            if ((string?)parameters[_variablesKey] is { Length: > 0, } sv)
            {
                variableSet = ParseVariables(sv);
            }

            if (extensions is null &&
                (string?)parameters[ExtensionsKey] is { Length: > 0, } se)
            {
                extensions = ParseJsonObject(se);
            }

            return new GraphQLRequest(
                document,
                queryId,
                queryHash,
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
    
    public GraphQLRequest ParsePersistedOperationRequestFromParams(string operationId, IQueryCollection parameters)
    {
        string? operationName = parameters[_operationNameKey];
        EnsureValidQueryId(operationId);
        
        try
        {
            IReadOnlyList<IReadOnlyDictionary<string, object?>>? variableSet = null;
            if ((string?)parameters[_variablesKey] is { Length: > 0, } sv)
            {
                variableSet = ParseVariables(sv);
            }
            
            IReadOnlyDictionary<string, object?>? extensions = null;
            if (extensions is null &&
                (string?)parameters[ExtensionsKey] is { Length: > 0, } se)
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

    private (string QueryHash, DocumentNode Document) ParseQueryString(string sourceText)
    {
        var length = checked(sourceText.Length * 4);
        byte[]? source = null;

        var sourceSpan = length <= GraphQLConstants.StackallocThreshold
            ? stackalloc byte[length]
            : source = ArrayPool<byte>.Shared.Rent(length);

        Utf8GraphQLParser.ConvertToBytes(sourceText, ref sourceSpan);
        var document = Utf8GraphQLParser.Parse(sourceSpan, _parserOptions);
        var queryHash = _documentHashProvider.ComputeHash(sourceSpan);

        if (source != null)
        {
            sourceSpan.Clear();
            ArrayPool<byte>.Shared.Return(source);
        }

        return (queryHash, document);
    }

    public IReadOnlyList<GraphQLRequest> ParseRequest(
        string operations)
        => EnsureValidQueryId(Parse(operations, _parserOptions, _documentCache, _documentHashProvider));

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
        
        return EnsureValidQueryId(requestParser.Parse());
    }
    
    private GraphQLRequest ParsePersistedOperationRequest(
        byte[] buffer,
        int bytesBuffered,
        string operationId)
    {
        var graphQLData = new ReadOnlySpan<byte>(buffer);
        graphQLData = graphQLData[..bytesBuffered];

        var requestParser = new Utf8GraphQLRequestParser(
            graphQLData,
            _parserOptions,
            _documentCache,
            _documentHashProvider);

        return requestParser.ParsePersistedOperation(operationId);
    }

    internal static IReadOnlyList<GraphQLRequest> EnsureValidQueryId(IReadOnlyList<GraphQLRequest> requests)
    {
        if (requests.Count == 1)
        {
            var request = requests[0];
            if (!string.IsNullOrWhiteSpace(request.QueryId))
            {
                EnsureValidQueryId(request.QueryId);
            }
            return requests;
        }

        foreach (var request in requests)
        {
            if (!string.IsNullOrWhiteSpace(request.QueryId))
            {
                EnsureValidQueryId(request.QueryId);
            }
        }
        return requests;
    }

    private static void EnsureValidQueryId(string queryId)
    {
        if (!OperationDocumentId.IsValidId(queryId))
        {
            throw ErrorHelper.InvalidQueryIdFormat();
        }
    }
}