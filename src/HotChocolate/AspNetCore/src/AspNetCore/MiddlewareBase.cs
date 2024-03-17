using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Language;
using HotChocolate.Utilities;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

/// <summary>
/// The Hot Chocolate ASP.NET core middleware base class.
/// </summary>
public class MiddlewareBase : IDisposable
{
    private readonly RequestDelegate _next;
    private readonly IHttpResponseFormatter _responseFormatter;
    private readonly RequestExecutorProxy _executorProxy;
    private GraphQLServerOptions? _options;
    private bool _disposed;

    protected MiddlewareBase(
        RequestDelegate next,
        IRequestExecutorResolver executorResolver,
        IHttpResponseFormatter responseFormatter,
        string schemaName)
    {
        if (executorResolver is null)
        {
            throw new ArgumentNullException(nameof(executorResolver));
        }

        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _responseFormatter = responseFormatter ??
            throw new ArgumentNullException(nameof(responseFormatter));
        SchemaName = schemaName;
        IsDefaultSchema = SchemaName.EqualsOrdinal(Schema.DefaultName);
        _executorProxy = new RequestExecutorProxy(executorResolver, schemaName);
    }

    /// <summary>
    /// Gets the name of the schema that this middleware serves up.
    /// </summary>
    protected string SchemaName { get; }

    /// <summary>
    /// Specifies if this middleware handles the default schema.
    /// </summary>
    protected bool IsDefaultSchema { get; }

    /// <summary>
    /// Gets the request executor proxy.
    /// </summary>
    protected RequestExecutorProxy ExecutorProxy => _executorProxy;

    /// <summary>
    /// Gets the response formatter.
    /// </summary>
    protected IHttpResponseFormatter ResponseFormatter => _responseFormatter;

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
        => _executorProxy.GetRequestExecutorAsync(cancellationToken);

    /// <summary>
    /// Gets the schema for this middleware.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the schema for this middleware.
    /// </returns>
    protected ValueTask<ISchema> GetSchemaAsync(CancellationToken cancellationToken)
        => _executorProxy.GetSchemaAsync(cancellationToken);

    protected ValueTask WriteResultAsync(
        HttpContext context,
        IExecutionResult result,
        AcceptMediaType[] acceptMediaTypes,
        HttpStatusCode? statusCode = null)
        => _responseFormatter.FormatAsync(
            context.Response,
            result,
            acceptMediaTypes,
            statusCode,
            context.RequestAborted);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected GraphQLRequestFlags CreateRequestFlags(AcceptMediaType[] acceptMediaTypes)
        => _responseFormatter.CreateRequestFlags(acceptMediaTypes);

    protected static async Task<IExecutionResult> ExecuteSingleAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        IHttpRequestInterceptor requestInterceptor,
        IServerDiagnosticEvents diagnosticEvents,
        GraphQLRequest request,
        GraphQLRequestFlags flags)
    {
        diagnosticEvents.StartSingleRequest(context, request);

        var requestBuilder = OperationRequestBuilder.From(request);
        requestBuilder.SetFlags(flags);

        await requestInterceptor.OnCreateAsync(
            context,
            requestExecutor,
            requestBuilder,
            context.RequestAborted);

        return await requestExecutor.ExecuteAsync(
            requestBuilder.Build(),
            context.RequestAborted);
    }

    protected static async Task<IResponseStream> ExecuteOperationBatchAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        IHttpRequestInterceptor requestInterceptor,
        IServerDiagnosticEvents diagnosticEvents,
        GraphQLRequest request,
        GraphQLRequestFlags flags,
        IReadOnlyList<string> operationNames)
    {
        diagnosticEvents.StartOperationBatchRequest(context, request, operationNames);

        var requestBatch = new IOperationRequest[operationNames.Count];

        for (var i = 0; i < operationNames.Count; i++)
        {
            var requestBuilder = OperationRequestBuilder.From(request);
            requestBuilder.SetOperationName(operationNames[i]);
            requestBuilder.SetFlags(flags);

            await requestInterceptor.OnCreateAsync(
                context,
                requestExecutor,
                requestBuilder,
                context.RequestAborted);

            requestBatch[i] = requestBuilder.Build();
        }

        return await requestExecutor.ExecuteBatchAsync(
            new OperationRequestBatch(requestBatch, services: context.RequestServices),
            cancellationToken: context.RequestAborted);
    }

    protected static async Task<IResponseStream> ExecuteBatchAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        IHttpRequestInterceptor requestInterceptor,
        IServerDiagnosticEvents diagnosticEvents,
        IReadOnlyList<GraphQLRequest> requests,
        GraphQLRequestFlags flags)
    {
        diagnosticEvents.StartBatchRequest(context, requests);

        var requestBatch = new IOperationRequest[requests.Count];

        for (var i = 0; i < requests.Count; i++)
        {
            var requestBuilder = OperationRequestBuilder.From(requests[i]);
            requestBuilder.SetFlags(flags);

            await requestInterceptor.OnCreateAsync(
                context,
                requestExecutor,
                requestBuilder,
                context.RequestAborted);

            requestBatch[i] = requestBuilder.Build();
        }

        return await requestExecutor.ExecuteBatchAsync(
            new OperationRequestBatch(requestBatch, services: context.RequestServices),
            cancellationToken: context.RequestAborted);
    }

    protected static RequestContentType ParseContentType(HttpContext context)
    {
        if (context.Items.TryGetValue(nameof(RequestContentType), out var value) &&
            value is RequestContentType contentType)
        {
            return contentType;
        }

        var span = context.Request.ContentType.AsSpan();

        if (span.StartsWith(ContentType.JsonSpan()))
        {
            context.Items[nameof(RequestContentType)] = RequestContentType.Json;
            return RequestContentType.Json;
        }

        if (span.StartsWith(ContentType.MultiPartFormSpan()))
        {
            context.Items[nameof(RequestContentType)] = RequestContentType.Form;
            return RequestContentType.Form;
        }

        context.Items[nameof(RequestContentType)] = RequestContentType.None;
        return RequestContentType.None;
    }

    protected GraphQLServerOptions GetOptions(HttpContext context)
    {
        if (_options is not null)
        {
            return _options;
        }

        _options = context.GetGraphQLServerOptions() ?? new GraphQLServerOptions();
        return _options;
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
