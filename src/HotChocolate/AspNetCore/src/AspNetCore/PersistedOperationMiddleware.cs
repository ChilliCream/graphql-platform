using System.Net;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HotChocolate.AspNetCore;

internal static class PersistedOperationMiddleware
{
    internal static void MapPersistedOperationMiddleware(
        this RouteGroupBuilder groupBuilder,
        string schemaName,
        bool requireOperationName)
    {
        var state = new State(schemaName);

        groupBuilder
            .MapGet(
                "/{OperationId}",
                ([AsParameters] Services services, string operationId)
                    => ExecuteGetRequestAsync(state, services, operationId, null, requireOperationName));

        groupBuilder
            .MapGet(
                "/{OperationId}/{OperationName}",
                ([AsParameters] Services services, string operationId, string operationName)
                    => ExecuteGetRequestAsync(state, services, operationId, operationName, requireOperationName));

        groupBuilder
            .MapPost(
                "/{OperationId}",
                ([AsParameters] Services services, string operationId)
                    => ExecutePostRequestAsync(state, services, operationId, null, requireOperationName));

        groupBuilder
            .MapPost(
                "/{OperationId}/{OperationName}",
                ([AsParameters] Services services, string operationId, string operationName)
                    => ExecutePostRequestAsync(state, services, operationId, operationName, requireOperationName));
    }

    private static async Task ExecuteGetRequestAsync(
        State state,
        Services services,
        string operationId,
        string? operationName,
        bool requireOperationName)
    {
        HttpStatusCode? statusCode;
        IExecutionResult? result;

        // first we validate the accept headers.
        var validationResult =
            MiddlewareHelper.ValidateAcceptContentType(
                services.Context,
                services.ResponseFormatter,
                services.DiagnosticEvents);

        var acceptMediaTypes = validationResult.AcceptMediaTypes;

        if (!validationResult.IsValid)
        {
            statusCode = validationResult.StatusCode.Value;
            result = validationResult.Error;
            goto HANDLE_RESULT;
        }

        // validate if the operation name is required.
        if(requireOperationName && string.IsNullOrWhiteSpace(operationName))
        {
            statusCode = HttpStatusCode.BadRequest;
            result = ErrorHelper.OperationNameRequired();
            goto HANDLE_RESULT;
        }

        // next we parse the GraphQL request.
        var executor = state.CurrentExecutor
            ?? await state.GetExecutorAsync(services.ExecutorResolver, services.RequestAborted);
        var errorHandler = executor.GetErrorHandler();

        var parserResult =
            MiddlewareHelper.ParseVariablesAndExtensionsFromParams(
                operationId,
                operationName,
                services.Context,
                services.RequestParser,
                errorHandler,
                services.DiagnosticEvents);

        if (!parserResult.IsValid)
        {
            statusCode = parserResult.StatusCode.Value;
            result = parserResult.Error;
            goto HANDLE_RESULT;
        }

        // before we can execute the request we need to determine the request flags.
        var request = parserResult.Request!;
        var options = state.GetOptions(services.Context);
        var requestFlags =
            MiddlewareHelper.DetermineHttpGetRequestFlags(
                validationResult.RequestFlags,
                options);

        // next we will execute the request.
        var executionResult =
            await MiddlewareHelper.ExecuteRequestAsync(
                request,
                requestFlags,
                services.Context,
                executor,
                errorHandler,
                services.DiagnosticEvents);
        statusCode = executionResult.StatusCode;
        result = executionResult.Result;

        HANDLE_RESULT:
        await MiddlewareHelper.WriteResultAsync(
            result!,
            acceptMediaTypes,
            statusCode,
            services.Context,
            services.ResponseFormatter,
            services.DiagnosticEvents);
    }

    private static async Task ExecutePostRequestAsync(
        State state,
        Services services,
        string operationId,
        string? operationName,
        bool requireOperationName)
    {
        HttpStatusCode? statusCode;
        IExecutionResult? result;

        // first we validate the accept headers.
        var validationResult =
            MiddlewareHelper.ValidateAcceptContentType(
                services.Context,
                services.ResponseFormatter,
                services.DiagnosticEvents);

        var acceptMediaTypes = validationResult.AcceptMediaTypes;

        if (!validationResult.IsValid)
        {
            statusCode = validationResult.StatusCode.Value;
            result = validationResult.Error;
            goto HANDLE_RESULT;
        }

        // validate if the operation name is required.
        if(requireOperationName && string.IsNullOrWhiteSpace(operationName))
        {
            statusCode = HttpStatusCode.BadRequest;
            result = ErrorHelper.OperationNameRequired();
            goto HANDLE_RESULT;
        }

        // next we parse the GraphQL request.
        var executor = state.CurrentExecutor
            ?? await state.GetExecutorAsync(services.ExecutorResolver, services.RequestAborted);
        var errorHandler = executor.GetErrorHandler();

        var parserResult =
            await MiddlewareHelper.ParseSingleRequestFromBodyAsync(
                operationId,
                operationName,
                services.Context,
                services.RequestParser,
                errorHandler,
                services.DiagnosticEvents);

        if (!parserResult.IsValid)
        {
            statusCode = parserResult.StatusCode.Value;
            result = parserResult.Error;
            goto HANDLE_RESULT;
        }

        // after successfully parsing the request we now will attempt to execute the request.
        var executionResult =
            await MiddlewareHelper.ExecuteRequestAsync(
                parserResult.Request!,
                validationResult.RequestFlags,
                services.Context,
                executor,
                errorHandler,
                services.DiagnosticEvents);
        statusCode = executionResult.StatusCode;
        result = executionResult.Result;

        HANDLE_RESULT:
        await MiddlewareHelper.WriteResultAsync(
            result!,
            acceptMediaTypes,
            statusCode,
            services.Context,
            services.ResponseFormatter,
            services.DiagnosticEvents);
    }

    private sealed class Services(
        HttpContext context,
        IRequestExecutorResolver executorResolver,
        IHttpResponseFormatter responseFormatter,
        IHttpRequestParser requestParser,
        IServerDiagnosticEvents diagnosticEvents)
    {
        public HttpContext Context { get; } = context;

        public IRequestExecutorResolver ExecutorResolver { get; } = executorResolver;

        public IHttpResponseFormatter ResponseFormatter { get; } = responseFormatter;

        public IHttpRequestParser RequestParser { get; } = requestParser;

        public IServerDiagnosticEvents DiagnosticEvents { get; } = diagnosticEvents;

        public CancellationToken RequestAborted => Context.RequestAborted;
    }

    private class State(string schemaName)
    {
        private RequestExecutorProxy? _proxy;
        private GraphQLServerOptions? _options;

        public IRequestExecutor? CurrentExecutor => _proxy?.CurrentExecutor;

        public ValueTask<IRequestExecutor> GetExecutorAsync(
            IRequestExecutorResolver resolver,
            CancellationToken cancellationToken)
        {
            _proxy ??= new RequestExecutorProxy(resolver, schemaName);
            return _proxy.GetRequestExecutorAsync(cancellationToken);
        }

        public GraphQLServerOptions GetOptions(HttpContext context)
        {
            if (_options is not null)
            {
                return _options;
            }

            _options = context.GetGraphQLServerOptions() ?? new GraphQLServerOptions();
            return _options;
        }
    }
}
