// ReSharper disable RedundantSuppressNullableWarningExpression

using System.Buffers;
using System.Text;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Buffers;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using static HotChocolate.AspNetCore.Utilities.ThrowHelper;
using static HotChocolate.Language.Utf8GraphQLRequestParser;

namespace HotChocolate.AspNetCore.Parsers;

internal sealed class DefaultHttpRequestParser : IHttpRequestParser
{
    private const int MinRequestSize = 256;
    internal const string QueryIdKey = "id";
    private const string OperationNameKey = "operationName";
    private const string OnErrorKey = "onError";
    internal const string QueryKey = "query";
    private const string VariablesKey = "variables";
    internal const string ExtensionsKey = "extensions";

    private static readonly Encoding s_utf8 = Encoding.UTF8;
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
            const int chunkSize = 256;
            using var writer = new PooledArrayWriter();
            var read = 0;

            do
            {
                var memory = writer.GetMemory(chunkSize);
                read = await requestBody.ReadAsync(memory, cancellationToken).ConfigureAwait(false);
                writer.Advance(read);

                if (_maxRequestSize < writer.Length)
                {
                    throw DefaultHttpRequestParser_MaxRequestSizeExceeded();
                }
            } while (read == chunkSize);

            if (writer.Length == 0)
            {
                throw DefaultHttpRequestParser_RequestIsEmpty();
            }

            return ParsePersistedOperationRequest(writer.WrittenSpan, documentId, operationName);
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
        string? onError = parameters[OnErrorKey];
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

            if (extensions is null
                && (string?)parameters[ExtensionsKey] is { Length: > 0 } se)
            {
                extensions = ParseJsonObject(se);
            }

            ErrorHandlingMode? errorHandlingMode = null;
            if (!string.IsNullOrEmpty(onError))
            {
                errorHandlingMode = ParseErrorHandlingMode(onError);
            }

            return new GraphQLRequest(
                document,
                queryId,
                documentHash,
                operationName,
                errorHandlingMode,
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
            if (extensions is null
                && (string?)parameters[ExtensionsKey] is { Length: > 0 } se)
            {
                extensions = ParseJsonObject(se);
            }

            string? onError = parameters[OnErrorKey];

            ErrorHandlingMode? errorHandlingMode = null;
            if (!string.IsNullOrEmpty(onError))
            {
                errorHandlingMode = ParseErrorHandlingMode(onError);
            }

            return new GraphQLRequest(
                null,
                operationId,
                null,
                operationName,
                errorHandlingMode,
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

        var written = Encoding.UTF8.GetBytes(sourceText, sourceSpan);
        sourceSpan = sourceSpan[..written];

        var document = Utf8GraphQLParser.Parse(sourceSpan, _parserOptions);
        var documentHash = _documentHashProvider.ComputeHash(sourceSpan);

        if (source != null)
        {
            sourceSpan.Clear();
            ArrayPool<byte>.Shared.Return(source);
        }

        return (documentHash, document);
    }

    private ErrorHandlingMode? ParseErrorHandlingMode(string onError)
    {
        if (onError.Equals("PROPAGATE", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorHandlingMode.Propagate;
        }

        if (onError.Equals("NULL", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorHandlingMode.Null;
        }

        if (onError.Equals("HALT", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorHandlingMode.Halt;
        }

        return null;
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
            const int chunkSize = 256;
            using var writer = new PooledArrayWriter();
            int read;

            do
            {
                var memory = writer.GetMemory(chunkSize);
                read = await stream.ReadAsync(memory, cancellationToken).ConfigureAwait(false);
                writer.Advance(read);

                if (_maxRequestSize < writer.Length)
                {
                    throw DefaultHttpRequestParser_MaxRequestSizeExceeded();
                }
            } while (read == chunkSize);

            if (writer.Length == 0)
            {
                throw DefaultHttpRequestParser_RequestIsEmpty();
            }

            return ParseRequest(writer.WrittenSpan);
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
        ReadOnlySpan<byte> request)
    {
        var requestParser = new Utf8GraphQLRequestParser(
            request,
            _parserOptions,
            _documentCache,
            _documentHashProvider);

        return requestParser.Parse();
    }

    private GraphQLRequest ParsePersistedOperationRequest(
        ReadOnlySpan<byte> request,
        string documentId,
        string? operationName)
    {
        var requestParser = new Utf8GraphQLRequestParser(
            request,
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
