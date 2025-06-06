using System.Net;
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Serialization;
using static HotChocolate.AspNetCore.Serialization.DefaultHttpRequestParser;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

public sealed class HttpGetMiddleware : MiddlewareBase
{
    private readonly IHttpRequestParser _requestParser;
    private readonly IServerDiagnosticEvents _diagnosticEvents;

    public HttpGetMiddleware(
        HttpRequestDelegate next,
        IRequestExecutorProvider executorResolver,
        IRequestExecutorEvents executorEvents,
        IHttpResponseFormatter responseFormatter,
        IHttpRequestParser requestParser,
        IServerDiagnosticEvents diagnosticEvents,
        string schemaName)
        : base(next, executorResolver, executorEvents, responseFormatter, schemaName)
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
        HttpStatusCode? statusCode;
        IExecutionResult? result;

        // first we validate the accept headers.
        var validationResult =
            MiddlewareHelper.ValidateAcceptContentType(
                context,
                ResponseFormatter,
                _diagnosticEvents);

        var acceptMediaTypes = validationResult.AcceptMediaTypes;

        if (!validationResult.IsValid)
        {
            statusCode = validationResult.StatusCode.Value;
            result = validationResult.Error;
            goto HANDLE_RESULT;
        }

        // next we parse the GraphQL request.
        var executor = ExecutorProxy.CurrentExecutor ??
            await ExecutorProxy.GetExecutorAsync(context.RequestAborted);
        var errorHandler = executor.GetErrorHandler();

        var parserResult =
            MiddlewareHelper.ParseRequestFromParams(
                context,
                _requestParser,
                errorHandler,
                _diagnosticEvents);

        if (!parserResult.IsValid)
        {
            statusCode = parserResult.StatusCode.Value;
            result = parserResult.Error;
            goto HANDLE_RESULT;
        }

        // before we can execute the request we need to determine the request flags.
        var request = parserResult.Request!;
        var options = GetOptions(context);
        var requestFlags =
            MiddlewareHelper.DetermineHttpGetRequestFlags(
                validationResult.RequestFlags,
                options);

        // next we will execute the request.
        var executionResult =
            await MiddlewareHelper.ExecuteRequestAsync(
                request,
                requestFlags,
                context,
                executor,
                errorHandler,
                _diagnosticEvents);
        statusCode = executionResult.StatusCode;
        result = executionResult.Result;

        HANDLE_RESULT:
        await MiddlewareHelper.WriteResultAsync(
            result!,
            acceptMediaTypes,
            statusCode,
            context,
            ResponseFormatter,
            _diagnosticEvents);
    }
}
