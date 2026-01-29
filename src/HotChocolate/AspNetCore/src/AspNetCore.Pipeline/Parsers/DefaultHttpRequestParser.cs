// ReSharper disable RedundantSuppressNullableWarningExpression

using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using HotChocolate.AspNetCore.Utilities;
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

    public async ValueTask<GraphQLRequest[]> ParseRequestAsync(
        PipeReader requestBody,
        CancellationToken cancellationToken)
    {
        try
        {
            ReadResult result;

            do
            {
                result = await requestBody.ReadAsync(cancellationToken);

                if (result.Buffer.Length > _maxRequestSize)
                {
                    requestBody.AdvanceTo(result.Buffer.End);
                    throw new GraphQLRequestException("Request size exceeds maximum allowed size.");
                }

                // We tell the pipe that we've examined everything but consumed nothing yet.
                requestBody.AdvanceTo(result.Buffer.Start, result.Buffer.End);
            }
            while (result is { IsCompleted: false, IsCanceled: false });

            if (result.IsCanceled)
            {
                throw new OperationCanceledException();
            }

            var requestParser = new Utf8GraphQLRequestParser(
                _parserOptions,
                _documentCache,
                _documentHashProvider);

            var requests = requestParser.Parse(result.Buffer);

            // Mark all data as consumed
            requestBody.AdvanceTo(result.Buffer.End);

            return requests;
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

    public async ValueTask<GraphQLRequest> ParsePersistedOperationRequestAsync(
        string documentId,
        string? operationName,
        PipeReader requestBody,
        CancellationToken cancellationToken)
    {
        if (!OperationDocumentId.TryParse(documentId, out var parsedDocumentId))
        {
            throw new InvalidGraphQLRequestException(
                "The GraphQL document ID contains invalid characters.");
        }

        try
        {
            ReadResult result;

            do
            {
                result = await requestBody.ReadAsync(cancellationToken);

                if (result.Buffer.Length > _maxRequestSize)
                {
                    requestBody.AdvanceTo(result.Buffer.End);
                    throw new GraphQLRequestException("Request size exceeds maximum allowed size.");
                }

                // We tell the pipe that we've examined everything but consumed nothing yet.
                requestBody.AdvanceTo(result.Buffer.Start, result.Buffer.End);
            }
            while (result is { IsCompleted: false, IsCanceled: false });

            if (result.IsCanceled)
            {
                throw new OperationCanceledException();
            }

            var requestParser = new Utf8GraphQLRequestParser(
                _parserOptions,
                _documentCache,
                _documentHashProvider);

            var request = requestParser.ParsePersistedOperation(parsedDocumentId, operationName, result.Buffer);

            // Mark all data as consumed
            requestBody.AdvanceTo(result.Buffer.End);

            return request;
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
        JsonDocument? extensions = null;

        // if we have no query or query id, we cannot execute anything.
        if (string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(queryId))
        {
            // so, if we do not find a top-level query or top-level id, we will try to parse
            // the extensions and look in the extensions for Apollo's active persisted
            // query extensions.
            if ((string?)parameters[ExtensionsKey] is { Length: > 0 } se)
            {
                extensions = JsonDocument.Parse(se);
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

            JsonDocument? variableSet = null;
            if ((string?)parameters[VariablesKey] is { Length: > 0 } sv)
            {
                variableSet = JsonDocument.Parse(sv);
            }

            if (extensions is null
                && (string?)parameters[ExtensionsKey] is { Length: > 0 } se)
            {
                extensions = JsonDocument.Parse(se);
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
            JsonDocument? variableSet = null;
            if ((string?)parameters[VariablesKey] is { Length: > 0 } sv)
            {
                variableSet = JsonDocument.Parse(sv);
            }

            JsonDocument? extensions = null;
            if (extensions is null
                && (string?)parameters[ExtensionsKey] is { Length: > 0 } se)
            {
                extensions = JsonDocument.Parse(se);
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

    private static ErrorHandlingMode? ParseErrorHandlingMode(string onError)
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

    public GraphQLRequest[] ParseRequest(string sourceText)
    {
        byte[]? rented = null;
        var maxLength = s_utf8.GetMaxByteCount(sourceText.Length);
        var span = maxLength < 256 ? stackalloc byte[256] : rented = ArrayPool<byte>.Shared.Rent(maxLength);

        try
        {
            s_utf8.GetBytes(sourceText, span);
            return Parse(span, _parserOptions, _documentCache, _documentHashProvider);
        }
        catch (OperationIdFormatException)
        {
            throw ErrorHelper.InvalidOperationIdFormat();
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static void EnsureValidDocumentId(string documentId)
    {
        if (!OperationDocumentId.IsValidId(documentId))
        {
            throw ErrorHelper.InvalidOperationIdFormat();
        }
    }
}
