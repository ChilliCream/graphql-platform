using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Language;
using static HotChocolate.AspNetCore.Serialization.DefaultHttpRequestParser;
using static HotChocolate.Execution.GraphQLRequestFlags;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

public sealed class HttpGetMiddleware : MiddlewareBase
{
    private readonly IHttpRequestParser _requestParser;
    private readonly IServerDiagnosticEvents _diagnosticEvents;

    public HttpGetMiddleware(
        HttpRequestDelegate next,
        IRequestExecutorResolver executorResolver,
        IHttpResponseFormatter responseFormatter,
        IHttpRequestParser requestParser,
        IServerDiagnosticEvents diagnosticEvents,
        string schemaName)
        : base(next, executorResolver, responseFormatter, schemaName)
    {
        _requestParser = requestParser ??
            throw new ArgumentNullException(nameof(requestParser));
        _diagnosticEvents = diagnosticEvents ??
            throw new ArgumentNullException(nameof(diagnosticEvents));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method))
        {
            var options = GetOptions(context);

            if (options.EnableGetRequests &&

                // Verify that the request is relevant to this middleware.
                (context.Request.Query.ContainsKey(QueryKey) ||
                    context.Request.Query.ContainsKey(QueryIdKey) ||
                    context.Request.Query.ContainsKey(ExtensionsKey)) &&

                // Allow ALL GET requests if we do NOT enforce preflight
                // requests on HTTP GraphQL GET requests
                (!options.EnforceGetRequestsPreflightHeader ||

                    // Allow HTTP GraphQL GET requests if the preflight header is set.
                    context.Request.Headers.ContainsKey(HttpHeaderKeys.Preflight) ||

                    // Allow HTTP GraphQL GET requests if the content type is set to
                    // application/json.
                    ParseContentType(context) is RequestContentType.Json))
            {
                if (!IsDefaultSchema)
                {
                    context.Items[WellKnownContextData.SchemaName] = SchemaName;
                }

                using (_diagnosticEvents.ExecuteHttpRequest(context, HttpRequestKind.HttpGet))
                {
                    await HandleRequestAsync(context);
                }

                return;
            }
        }

        // if the request is not a get request or if the content type is not correct
        // we will just invoke the next middleware and do nothing.
        await NextAsync(context);
    }

    private async Task HandleRequestAsync(HttpContext context)
    {
        HttpStatusCode? statusCode = null;
        IExecutionResult? result;

        // first we need to get the request executor to be able to execute requests.
        var requestExecutor = await GetExecutorAsync(context.RequestAborted);
        var requestInterceptor = requestExecutor.GetRequestInterceptor();
        var errorHandler = requestExecutor.GetErrorHandler();
        context.Items[WellKnownContextData.RequestExecutor] = requestExecutor;

        // next we will inspect the accept headers and determine if we can execute this request.
        var headerResult = HeaderUtilities.GetAcceptHeader(context.Request);
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
            _diagnosticEvents.HttpRequestError(context, errors[0]);
            goto HANDLE_RESULT;
        }

        var requestFlags = CreateRequestFlags(headerResult.AcceptMediaTypes);

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
            _diagnosticEvents.HttpRequestError(context, error);
            goto HANDLE_RESULT;
        }

        // next we parse the GraphQL request.
        GraphQLRequest request;

        using (_diagnosticEvents.ParseHttpRequest(context))
        {
            try
            {
                request = _requestParser.ReadParamsRequest(context.Request.Query);
            }
            catch (GraphQLRequestException ex)
            {
                // A GraphQL request exception is thrown if the HTTP request body couldn't be
                // parsed. In this case we will return HTTP status code 400 and return a
                // GraphQL error result.
                statusCode = HttpStatusCode.BadRequest;
                var errors = errorHandler.Handle(ex.Errors);
                result = QueryResultBuilder.CreateError(errors);
                _diagnosticEvents.ParserErrors(context, errors);
                goto HANDLE_RESULT;
            }
            catch (Exception ex)
            {
                statusCode = HttpStatusCode.InternalServerError;
                var error = errorHandler.CreateUnexpectedError(ex).Build();
                result = QueryResultBuilder.CreateError(error);
                _diagnosticEvents.HttpRequestError(context, error);
                goto HANDLE_RESULT;
            }
        }

        // after successfully parsing the request we now will attempt to execute the request.
        try
        {
            var options = GetOptions(context);

            if (options is null or { AllowedGetOperations: AllowedGetOperations.Query, })
            {
                requestFlags = (requestFlags & AllowStreams) == AllowStreams
                    ? AllowQuery | AllowStreams
                    : AllowQuery;
            }
            else
            {
                var flags = options.AllowedGetOperations;
                var newRequestFlags = GraphQLRequestFlags.None;

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

                if((requestFlags & AllowStreams) == AllowStreams)
                {
                    newRequestFlags |= AllowStreams;
                }

                requestFlags = newRequestFlags;
            }

            result = await ExecuteSingleAsync(
                context,
                requestExecutor,
                requestInterceptor,
                _diagnosticEvents,
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
            if (context.RequestAborted.IsCancellationRequested)
            {
                return;
            }

            // in any case we will have a valid GraphQL result at this point that can be written
            // to the HTTP response stream.
            Debug.Assert(result is not null, "No GraphQL result was created.");

            if (result is IQueryResult queryResult)
            {
                formatScope = _diagnosticEvents.FormatHttpResponse(context, queryResult);
            }

            await WriteResultAsync(
                context,
                result,
                acceptMediaTypes,
                statusCode);
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
}
