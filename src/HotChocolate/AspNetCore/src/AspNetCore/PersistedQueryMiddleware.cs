#if NET8_0_OR_GREATER
using System.Diagnostics;
using System.Net;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Language;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static HotChocolate.Execution.GraphQLRequestFlags;

namespace HotChocolate.AspNetCore;

internal static class PersistedQueryMiddleware
{
    internal static void MapPersistedQueryMiddleware(this RouteGroupBuilder groupBuilder, string schemaName)
    {
        var state = new State(schemaName);

        groupBuilder
            .MapGet(
                "/q/{OperationId}/{DisplayName}",
                ([AsParameters] Services services, string operationId, string displayName)
                    => ExecuteGetRequestAsync(state, services, operationId, displayName));
    }

    private static async Task ExecuteGetRequestAsync(
        State state,
        Services services,
        string operationId,
        string displayName)
    {
        HttpStatusCode? statusCode = null;
        IExecutionResult? result;
        
        // first  we will inspect the accept headers and determine if we can execute this request.
        var headerResult = HeaderUtilities.GetAcceptHeader(services.Context.Request);
        var acceptMediaTypes = headerResult.AcceptMediaTypes;

        // if we cannot parse all media types that we provided we will fail the request
        // with a 400 Bad Request.
        if (headerResult.HasError)
        {
            // in this case accept headers were specified and we will
            // respond with proper error codes
            acceptMediaTypes = HeaderUtilities.GraphQLResponseContentTypes;
            statusCode = HttpStatusCode.BadRequest;

            var errors = headerResult.ErrorResult.Errors!;
            result = headerResult.ErrorResult;
            services.DiagnosticEvents.HttpRequestError(services.Context, errors[0]);
            goto HANDLE_RESULT;
        }
        
        var requestFlags = services.ResponseFormatter.CreateRequestFlags(headerResult.AcceptMediaTypes);
        
        // if the request defines accept header values of which we cannot handle any provided
        // media type then we will fail the request with 406 Not Acceptable.
        if (requestFlags is None)
        {
            // in this case accept headers were specified and we will
            // respond with proper error codes
            acceptMediaTypes = HeaderUtilities.GraphQLResponseContentTypes;
            statusCode = HttpStatusCode.NotAcceptable;

            var error = ErrorHelper.NoSupportedAcceptMediaType();
            result = QueryResultBuilder.CreateError(error);
            services.DiagnosticEvents.HttpRequestError(services.Context, error);
            goto HANDLE_RESULT;
        }

        var executor = state.CurrentExecutor;
        executor ??= await state.GetExecutorAsync(services.ExecutorResolver, services.CancellationToken);
        var errorHandler = executor.GetErrorHandler();
        services.Context.Items[WellKnownContextData.RequestExecutor] = executor;
        
        // next we parse the GraphQL request.
        GraphQLRequest request;
        using (services.DiagnosticEvents.ParseHttpRequest(services.Context))
        {
            try
            {
                request = services.RequestParser.ParseParamsVariablesAndExtensions(
                    operationId,
                    services.Context.Request.Query);
            }
            catch (GraphQLRequestException ex)
            {
                // A GraphQL request exception is thrown if the HTTP request body couldn't be
                // parsed. In this case we will return HTTP status code 400 and return a
                // GraphQL error result.
                statusCode = HttpStatusCode.BadRequest;
                var errors = errorHandler.Handle(ex.Errors);
                result = QueryResultBuilder.CreateError(errors);
                services.DiagnosticEvents.ParserErrors(services.Context, errors);
                goto HANDLE_RESULT;
            }
            catch (Exception ex)
            {
                statusCode = HttpStatusCode.InternalServerError;
                var error = errorHandler.CreateUnexpectedError(ex).Build();
                result = QueryResultBuilder.CreateError(error);
                services.DiagnosticEvents.HttpRequestError(services.Context, error);
                goto HANDLE_RESULT;
            }
        }

        // after successfully parsing the request we now will attempt to execute the request.
        try
        {
            var options = state.GetOptions(services.Context);

            if (options is null or { AllowedGetOperations: AllowedGetOperations.Query, })
            {
                requestFlags = (requestFlags & AllowStreams) == AllowStreams
                    ? AllowQuery | AllowStreams
                    : AllowQuery;
            }
            else
            {
                var flags = options.AllowedGetOperations;
                var newRequestFlags = None;

                if ((flags & AllowedGetOperations.Query) == AllowedGetOperations.Query)
                {
                    newRequestFlags |= AllowQuery;
                }

                if ((flags & AllowedGetOperations.Mutation) == AllowedGetOperations.Mutation)
                {
                    newRequestFlags |= AllowMutation;
                }

                if ((flags & AllowedGetOperations.Subscription) == AllowedGetOperations.Subscription &&
                    (requestFlags & AllowSubscription) == AllowSubscription)
                {
                    newRequestFlags |= AllowSubscription;
                }

                if ((requestFlags & AllowStreams) == AllowStreams)
                {
                    newRequestFlags |= AllowStreams;
                }

                requestFlags = newRequestFlags;
            }

            result = await MiddlewareBase.ExecuteSingleAsync(
                services.Context,
                executor,
                executor.GetRequestInterceptor(),
                services.DiagnosticEvents,
                request,
                requestFlags);
        }
        catch (GraphQLException ex)
        {
            // This allows extensions to throw GraphQL exceptions in the GraphQL interceptor.
            statusCode = null; // we let the serializer determine the status code.
            result = QueryResultBuilder.CreateError(ex.Errors);
        }
        catch (Exception ex)
        {
            statusCode = HttpStatusCode.InternalServerError;
            var error = errorHandler.CreateUnexpectedError(ex).Build();
            result = QueryResultBuilder.CreateError(error);
        }

        HANDLE_RESULT:
        IDisposable? formatScope = null;

        try
        {
            // if cancellation is requested we will not try to attempt to write the result to the
            // response stream.
            if (services.Context.RequestAborted.IsCancellationRequested)
            {
                return;
            }

            // in any case we will have a valid GraphQL result at this point that can be written
            // to the HTTP response stream.
            Debug.Assert(result is not null, "No GraphQL result was created.");

            if (result is IQueryResult queryResult)
            {
                formatScope = services.DiagnosticEvents.FormatHttpResponse(services.Context, queryResult);
            }
            
            await services.ResponseFormatter.FormatAsync(
                services.Context.Response,
                result,
                acceptMediaTypes,
                statusCode,
                services.CancellationToken);
        }
        finally
        {
            // we must dispose the diagnostic scope first.
            formatScope?.Dispose();

            // query results use pooled memory an need to be disposed after we have
            // used them.
            if (result is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }

            if (result is not null)
            {
                await result.DisposeAsync();
            }
        }
    }

    private sealed class Services(
        HttpContext context,
        IRequestExecutorResolver executorResolver,
        IHttpResponseFormatter responseFormatter,
        IHttpRequestParser requestParser,
        IServerDiagnosticEvents diagnosticEvents,
        CancellationToken cancellationToken)
    {
        public HttpContext Context { get; } = context;

        public IRequestExecutorResolver ExecutorResolver { get; } = executorResolver;

        public IHttpResponseFormatter ResponseFormatter { get; } = responseFormatter;

        public IHttpRequestParser RequestParser { get; } = requestParser;

        public IServerDiagnosticEvents DiagnosticEvents { get; } = diagnosticEvents;

        public CancellationToken CancellationToken { get; } = cancellationToken;
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
#endif