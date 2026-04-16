#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.IO.Pipelines;
using System.Net;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Parsers;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.PersistedOperations;
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
    private readonly bool _skipDocumentBody;
    private readonly IError? _operationNotAllowedError;

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
        var persistedOps = executor.Schema.Services.GetService<PersistedOperationOptions>();
        _skipDocumentBody = persistedOps is { OnlyAllowPersistedDocuments: true, AllowDocumentBody: false };
        _operationNotAllowedError = persistedOps?.OperationNotAllowedError;
    }

    public ISocketSessionInterceptor SocketSessionInterceptor => _socketSessionInterceptor;

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
        RequestFlags flags,
        GraphQLServerOptions options)
    {
        _diagnosticEvents.StartSingleRequest(context, request);

        var requestBuilder = OperationRequestBuilder.From(request);
        requestBuilder.SetFlags(flags);
        requestBuilder.SetServices(context.RequestServices);

        await _requestInterceptor.OnCreateAsync(context, _executor, requestBuilder, context.RequestAborted);

        var operationRequest = requestBuilder.Build();

        if (operationRequest is VariableBatchRequest variableBatch)
        {
            if (!options.Batching.HasFlag(AllowedBatching.VariableBatching))
            {
                var error = Handle(ErrorHelper.InvalidRequest());
                return OperationResult.FromError(error);
            }

            var maxBatchSize = options.MaxBatchSize;
            if (maxBatchSize > 0
                && variableBatch.VariableValues.Document.RootElement.GetArrayLength() > maxBatchSize)
            {
                var error = Handle(ErrorHelper.BatchSizeExceeded(maxBatchSize));
                return OperationResult.FromError(error);
            }
        }

        return await _executor.ExecuteAsync(operationRequest, context.RequestAborted);
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
            requestBuilder.SetServices(context.RequestServices);

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
        RequestFlags flags,
        GraphQLServerOptions options)
    {
        var maxBatchSize = options.MaxBatchSize;
        if (maxBatchSize > 0 && requests.Length > maxBatchSize)
        {
            var error = Handle(ErrorHelper.BatchSizeExceeded(maxBatchSize));
            throw new GraphQLException(error);
        }

        _diagnosticEvents.StartBatchRequest(context, requests);

        var requestBatch = new IOperationRequest[requests.Length];

        for (var i = 0; i < requests.Length; i++)
        {
            var requestBuilder = OperationRequestBuilder.From(requests[i]);
            requestBuilder.SetFlags(flags);
            requestBuilder.SetServices(context.RequestServices);

            await _requestInterceptor.OnCreateAsync(context, _executor, requestBuilder, context.RequestAborted);

            requestBatch[i] = requestBuilder.Build();
        }

        return await _executor.ExecuteBatchAsync(
            new OperationRequestBatch(requestBatch, services: context.RequestServices),
            cancellationToken: context.RequestAborted);
    }

    public async ValueTask<GraphQLRequest[]> ParseRequestAsync(
        PipeReader requestBody,
        CancellationToken cancellationToken)
    {
        var requests = await _requestParser.ParseRequestAsync(requestBody, _skipDocumentBody, cancellationToken);
        ThrowIfDocumentBodyNotAllowed(requests);
        return requests;
    }

    public async ValueTask<GraphQLRequest> ParsePersistedOperationRequestAsync(
        string documentId,
        string? operationName,
        PipeReader requestBody,
        CancellationToken cancellationToken)
    {
        var request = await _requestParser.ParsePersistedOperationRequestAsync(
            documentId, operationName, requestBody, _skipDocumentBody, cancellationToken);
        ThrowIfDocumentBodyNotAllowed(request);
        return request;
    }

    public GraphQLRequest ParseRequestFromParams(IQueryCollection parameters)
    {
        var request = _requestParser.ParseRequestFromParams(parameters, _skipDocumentBody);
        ThrowIfDocumentBodyNotAllowed(request);
        return request;
    }

    public GraphQLRequest ParsePersistedOperationRequestFromParams(
        string operationId,
        string? operationName,
        IQueryCollection parameters)
        => _requestParser.ParsePersistedOperationRequestFromParams(operationId, operationName, parameters);

    public GraphQLRequest[] ParseRequest(string sourceText)
    {
        var requests = _requestParser.ParseRequest(sourceText, _skipDocumentBody);
        ThrowIfDocumentBodyNotAllowed(requests);
        return requests;
    }

    private void ThrowIfDocumentBodyNotAllowed(GraphQLRequest request)
    {
        if (_skipDocumentBody && request.HasDocumentBody && request.DocumentId is null)
        {
            throw new GraphQLRequestException(_operationNotAllowedError!);
        }
    }

    private void ThrowIfDocumentBodyNotAllowed(GraphQLRequest[] requests)
    {
        foreach (var request in requests)
        {
            ThrowIfDocumentBodyNotAllowed(request);
        }
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
