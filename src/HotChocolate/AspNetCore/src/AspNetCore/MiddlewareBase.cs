using System.Net;
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Language;
using Microsoft.Net.Http.Headers;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

/// <summary>
/// The Hot Chocolate ASP.NET core middleware base class.
/// </summary>
public class MiddlewareBase : IDisposable
{
    private readonly RequestDelegate _next;
    private readonly IHttpResultSerializer _resultSerializer;
    private bool? _batching = null;
    private bool _disposed;

    protected MiddlewareBase(
        RequestDelegate next,
        IRequestExecutorResolver executorResolver,
        IHttpResultSerializer resultSerializer,
        NameString schemaName)
    {
        if (executorResolver == null)
        {
            throw new ArgumentNullException(nameof(executorResolver));
        }

        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _resultSerializer = resultSerializer ??
            throw new ArgumentNullException(nameof(resultSerializer));
        SchemaName = schemaName;
        IsDefaultSchema = SchemaName.Equals(Schema.DefaultName);
        ExecutorProxy = new RequestExecutorProxy(executorResolver, schemaName);
    }

    /// <summary>
    /// Gets the name of the schema that this middleware serves up.
    /// </summary>
    protected NameString SchemaName { get; }

    /// <summary>
    /// Specifies if this middleware handles the default schema.
    /// </summary>
    protected bool IsDefaultSchema { get; }

    /// <summary>
    /// Gets the request executor proxy.
    /// </summary>
    protected RequestExecutorProxy ExecutorProxy { get; }

    /// <summary>
    /// Invokes the next middleware in line.
    /// </summary>
    /// <param name="context">
    /// The <see cref="HttpContext"/>.
    /// </param>
    protected Task NextAsync(HttpContext context) => _next(context);

    /// <summary>
    /// Gets the request executor for this middleware.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the request executor for this middleware.
    /// </returns>
    protected ValueTask<IRequestExecutor> GetExecutorAsync(CancellationToken cancellationToken)
        => ExecutorProxy.GetRequestExecutorAsync(cancellationToken);

    /// <summary>
    /// Gets the schema for this middleware.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the schema for this middleware.
    /// </returns>
    protected async ValueTask<ISchema> GetSchemaAsync(CancellationToken cancellationToken)
    {
        IRequestExecutor requestExecutor = await GetExecutorAsync(cancellationToken);
        return requestExecutor.Schema;
    }

    protected ValueTask WriteResultAsync(
        HttpContext context,
        IExecutionResult result,
        HttpStatusCode? statusCode = null)
        => WriteResultAsync(context.Response, result, statusCode, context.RequestAborted);

    protected async ValueTask WriteResultAsync(
        HttpResponse response,
        IExecutionResult result,
        HttpStatusCode? statusCode,
        CancellationToken cancellationToken)
    {
        response.ContentType = _resultSerializer.GetContentType(result);
        response.StatusCode = (int)(statusCode ?? _resultSerializer.GetStatusCode(result));
        await _resultSerializer.SerializeAsync(result, response.Body, cancellationToken);
    }

    protected static async Task<IExecutionResult> ExecuteSingleAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        IHttpRequestInterceptor requestInterceptor,
        IServerDiagnosticEvents diagnosticEvents,
        GraphQLRequest request,
        OperationType[]? allowedOperations = null)
    {
        diagnosticEvents.StartSingleRequest(context, request);

        var requestBuilder = QueryRequestBuilder.From(request);
        requestBuilder.SetAllowedOperations(allowedOperations);

        await requestInterceptor.OnCreateAsync(
            context, requestExecutor, requestBuilder, context.RequestAborted);

        return await requestExecutor.ExecuteAsync(
            requestBuilder.Create(), context.RequestAborted);
    }

    protected static async Task<IResponseStream> ExecuteOperationBatchAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        IHttpRequestInterceptor requestInterceptor,
        IServerDiagnosticEvents diagnosticEvents,
        GraphQLRequest request,
        IReadOnlyList<string> operationNames)
    {
        diagnosticEvents.StartOperationBatchRequest(context, request, operationNames);

        var requestBatch = new IReadOnlyQueryRequest[operationNames.Count];

        for (var i = 0; i < operationNames.Count; i++)
        {
            var requestBuilder = QueryRequestBuilder.From(request);
            requestBuilder.SetOperation(operationNames[i]);

            await requestInterceptor.OnCreateAsync(
                context,
                requestExecutor,
                requestBuilder,
                context.RequestAborted);

            requestBatch[i] = requestBuilder.Create();
        }

        return await requestExecutor.ExecuteBatchAsync(
            requestBatch,
            cancellationToken: context.RequestAborted);
    }

    protected static async Task<IResponseStream> ExecuteBatchAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        IHttpRequestInterceptor requestInterceptor,
        IServerDiagnosticEvents diagnosticEvents,
        IReadOnlyList<GraphQLRequest> requests)
    {
        diagnosticEvents.StartBatchRequest(context, requests);

        var requestBatch = new IReadOnlyQueryRequest[requests.Count];

        for (var i = 0; i < requests.Count; i++)
        {
            var requestBuilder = QueryRequestBuilder.From(requests[i]);

            await requestInterceptor.OnCreateAsync(
                context, requestExecutor, requestBuilder, context.RequestAborted);

            requestBatch[i] = requestBuilder.Create();
        }

        return await requestExecutor.ExecuteBatchAsync(
            requestBatch,
            cancellationToken: context.RequestAborted);
    }

    protected static AllowedContentType ParseContentType(HttpContext context)
    {
        if (context.Items.TryGetValue(nameof(AllowedContentType), out var value) &&
            value is AllowedContentType contentType)
        {
            return contentType;
        }

        ReadOnlySpan<char> span = context.Request.ContentType.AsSpan();

        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] == ';')
            {
                span = span[..i];
                break;
            }
        }

        if (span.SequenceEqual(ContentType.JsonSpan()))
        {
            context.Items[nameof(AllowedContentType)] = AllowedContentType.Json;
            return AllowedContentType.Json;
        }

        if (span.SequenceEqual(ContentType.MultiPartSpan()))
        {
            context.Items[nameof(AllowedContentType)] = AllowedContentType.Form;
            return AllowedContentType.Form;
        }

        context.Items[nameof(AllowedContentType)] = AllowedContentType.None;
        return AllowedContentType.None;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            ExecutorProxy.Dispose();
            _disposed = true;
        }
    }
}
