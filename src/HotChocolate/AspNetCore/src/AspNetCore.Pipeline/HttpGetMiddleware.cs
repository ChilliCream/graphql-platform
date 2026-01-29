#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Net;
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Utilities;
using static HotChocolate.AspNetCore.Parsers.DefaultHttpRequestParser;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
public sealed class HttpGetMiddleware : MiddlewareBase
{
    public HttpGetMiddleware(
        HttpRequestDelegate next,
        HttpRequestExecutorProxy executor)
        : base(next, executor)
    {
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method))
        {
            var session = await Executor.GetOrCreateSessionAsync(context.RequestAborted);
            var options = GetOptions(context);

            if (options.EnableGetRequests

                // Verify that the request is relevant to this middleware.
                && (context.Request.Query.ContainsKey(QueryKey)
                    || context.Request.Query.ContainsKey(QueryIdKey)
                    || context.Request.Query.ContainsKey(ExtensionsKey))

                // Allow ALL GET requests if we do NOT enforce preflight
                // requests on HTTP GraphQL GET requests
                && (!options.EnforceGetRequestsPreflightHeader

                    // Allow HTTP GraphQL GET requests if the preflight header is set.
                    || context.Request.Headers.ContainsKey(HttpHeaderKeys.Preflight)

                    // Allow HTTP GraphQL GET requests if the content type is set to
                    // application/json.
                    || context.ParseContentType() is RequestContentType.Json))
            {
                using (session.DiagnosticEvents.ExecuteHttpRequest(context, HttpRequestKind.HttpGet))
                {
                    await HandleRequestAsync(context, session);
                }

                return;
            }
        }

        // if the request is not a get request or if the content type is not correct
        // we will just invoke the next middleware and do nothing.
        await NextAsync(context);
    }

    private async Task HandleRequestAsync(HttpContext context, ExecutorSession session)
    {
        HttpStatusCode? statusCode;
        IExecutionResult? result;

        // first we validate the accept headers.
        var validationResult = MiddlewareHelper.ValidateAcceptContentType(context, session);

        var acceptMediaTypes = validationResult.AcceptMediaTypes;

        if (!validationResult.IsValid)
        {
            statusCode = validationResult.StatusCode.Value;
            result = validationResult.Error;
            goto HANDLE_RESULT;
        }

        // next we parse the GraphQL request.
        var parserResult = MiddlewareHelper.ParseRequestFromParams(context, session);

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
                session);
        statusCode = executionResult.StatusCode;
        result = executionResult.Result;

HANDLE_RESULT:
        await MiddlewareHelper.WriteResultAsync(
            result!,
            acceptMediaTypes,
            statusCode,
            context,
            session);
    }
}
