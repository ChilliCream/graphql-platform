#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Net;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Parsers;
using HotChocolate.Features;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore;

public sealed class ExecutorSession
    : IRequestExecutor
    , IHttpRequestInterceptor
    , IErrorHandler
{
    private readonly IRequestExecutor _executor;
    private readonly IErrorHandler _errorHandler;
    private readonly IHttpRequestInterceptor _requestInterceptor;
    private readonly ISocketSessionInterceptor _socketSessionInterceptor;
    private readonly IHttpRequestParser _requestParser;
    private readonly IHttpResponseFormatter _responseFormatter;
    private readonly IServerDiagnosticEvents _diagnosticEvents;

    public ExecutorSession(IRequestExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(executor);

        _executor = executor;
        _errorHandler = executor.Schema.Services.GetRequiredService<IErrorHandler>();
        _requestInterceptor = executor.Schema.Services.GetRequiredService<IHttpRequestInterceptor>();
        _socketSessionInterceptor = executor.Schema.Services.GetRequiredService<ISocketSessionInterceptor>();
        _responseFormatter = executor.Schema.Services.GetRequiredService<IHttpResponseFormatter>();
        _requestParser = executor.Schema.Services.GetRequiredService<IHttpRequestParser>();
        _diagnosticEvents = executor.Schema.Services.GetRequiredService<IServerDiagnosticEvents>();
    }

    public ISocketSessionInterceptor SocketSessionInterceptor => _socketSessionInterceptor;

    public IHttpResponseFormatter ResponseFormatter => _responseFormatter;

    public IHttpRequestParser RequestParser => _requestParser;

    public IServerDiagnosticEvents DiagnosticEvents => _diagnosticEvents;

    public ulong Version => _executor.Version;

    public ISchemaDefinition Schema => _executor.Schema;

    public IFeatureCollection Features => _executor.Features;

    public Task<IExecutionResult> ExecuteAsync(
        IOperationRequest request,
        CancellationToken cancellationToken = default)
        => _executor.ExecuteAsync(request, cancellationToken);

    public Task<IResponseStream> ExecuteBatchAsync(
        OperationRequestBatch requestBatch,
        CancellationToken cancellationToken = default)
        => _executor.ExecuteBatchAsync(requestBatch, cancellationToken);

    public IError Handle(IError error)
        => _errorHandler.Handle(error);

    public ValueTask OnCreateAsync(
        HttpContext context,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
        => _requestInterceptor.OnCreateAsync(context, _executor, requestBuilder, cancellationToken);

    public ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
        => _requestInterceptor.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);

#if !NET9_0_OR_GREATER
    [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
    public async Task<IExecutionResult> ExecuteSingleAsync(
        HttpContext context,
        GraphQLRequest request,
        RequestFlags flags)
    {
        _diagnosticEvents.StartSingleRequest(context, request);

        var requestBuilder = OperationRequestBuilder.From(request);
        requestBuilder.SetFlags(flags);

        await _requestInterceptor.OnCreateAsync(context, _executor, requestBuilder, context.RequestAborted);

        return await _executor.ExecuteAsync(requestBuilder.Build(), context.RequestAborted);
    }

#if !NET9_0_OR_GREATER
    [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
    public async Task<IResponseStream> ExecuteOperationBatchAsync(
        HttpContext context,
        GraphQLRequest request,
        RequestFlags flags,
        IReadOnlyList<string> operationNames)
    {
        _diagnosticEvents.StartOperationBatchRequest(context, request, operationNames);

        var requestBatch = new IOperationRequest[operationNames.Count];

        for (var i = 0; i < operationNames.Count; i++)
        {
            var requestBuilder = OperationRequestBuilder.From(request);
            requestBuilder.SetOperationName(operationNames[i]);
            requestBuilder.SetFlags(flags);

            await _requestInterceptor.OnCreateAsync(context, _executor, requestBuilder, context.RequestAborted);

            requestBatch[i] = requestBuilder.Build();
        }

        return await _executor.ExecuteBatchAsync(
            new OperationRequestBatch(requestBatch, services: context.RequestServices),
            cancellationToken: context.RequestAborted);
    }

#if !NET9_0_OR_GREATER
    [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
    public async Task<IResponseStream> ExecuteBatchAsync(
        HttpContext context,
        GraphQLRequest[] requests,
        RequestFlags flags)
    {
        _diagnosticEvents.StartBatchRequest(context, requests);

        var requestBatch = new IOperationRequest[requests.Length];

        for (var i = 0; i < requests.Length; i++)
        {
            var requestBuilder = OperationRequestBuilder.From(requests[i]);
            requestBuilder.SetFlags(flags);

            await _requestInterceptor.OnCreateAsync(context, _executor, requestBuilder, context.RequestAborted);

            requestBatch[i] = requestBuilder.Build();
        }

        return await _executor.ExecuteBatchAsync(
            new OperationRequestBatch(requestBatch, services: context.RequestServices),
            cancellationToken: context.RequestAborted);
    }

    public ValueTask WriteResultAsync(
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

    public async Task WriteSchemaAsync(
        HttpContext context)
        => await _responseFormatter.FormatAsync(
            context.Response,
            Schema,
            Version,
            context.RequestAborted);

    public RequestFlags CreateRequestFlags(AcceptMediaType[] acceptMediaTypes)
        => _responseFormatter.CreateRequestFlags(acceptMediaTypes);
}
