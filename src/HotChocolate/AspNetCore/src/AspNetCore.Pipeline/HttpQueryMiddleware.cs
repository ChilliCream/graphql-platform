using System.Net;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Utilities;
using Microsoft.AspNetCore.Http;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

/// <summary>
/// Handles GraphQL requests sent with the HTTP QUERY method. The request body has the shape of
/// a POST body and carries exactly one query operation.
/// </summary>
public sealed class HttpQueryMiddleware : MiddlewareBase
{
    private const string BatchOperations = "batchOperations";

    public HttpQueryMiddleware(
        HttpRequestDelegate next,
        HttpRequestExecutorProxy executor,
        GraphQLServerOptions baseOptions)
        : base(next, executor, baseOptions)
    {
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsQuery(context.Request.Method)
            && GetOptions(context).EnableQueryRequests
            && context.ParseContentType() is RequestContentType.Json)
        {
            var session = await Executor.GetOrCreateSessionAsync(context.RequestAborted);

            using (session.DiagnosticEvents.ExecuteHttpRequest(context, HttpRequestKind.HttpQuery))
            {
                await HandleRequestAsync(context, session);
            }

            return;
        }

        // if the request is not a QUERY request the endpoint accepts, or if the content type is
        // not correct, we will just invoke the next middleware and do nothing.
        await NextAsync(context);
    }

    private static async Task HandleRequestAsync(HttpContext context, ExecutorSession session)
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

        // a QUERY request carries exactly one operation, so an operation batch is refused.
        if (context.Request.Query.ContainsKey(BatchOperations))
        {
            var refused = MiddlewareHelper.CreateBatchingRefusedResult(
                ErrorHelper.RequestBatchingNotSupportedForQuery(),
                context,
                session);
            statusCode = refused.StatusCode;
            result = refused.Error;
            goto HANDLE_RESULT;
        }

        // next we parse the GraphQL request.
        var parserResult = await MiddlewareHelper.ParseSingleRequestFromBodyAsync(context, session);

        if (!parserResult.IsValid)
        {
            statusCode = parserResult.StatusCode;
            result = parserResult.Error;
            goto HANDLE_RESULT;
        }

        // a QUERY request carries exactly one variable set, so a variable batch is refused.
        if (MiddlewareHelper.IsVariableBatch(parserResult.Request!))
        {
            var refused = MiddlewareHelper.CreateBatchingRefusedResult(
                ErrorHelper.VariableBatchingNotSupportedForQuery(),
                context,
                session);
            statusCode = refused.StatusCode;
            result = refused.Error;
            goto HANDLE_RESULT;
        }

        // before we can execute the request we need to determine the request flags.
        var requestFlags =
            MiddlewareHelper.DetermineHttpQueryRequestFlags(validationResult.RequestFlags);

        // next we will execute the request.
        var executionResult = await MiddlewareHelper.ExecuteRequestAsync(
            parserResult.Request!,
            requestFlags,
            context,
            session);

        // a mutation or subscription the executor refused before it ran is answered 422.
        statusCode = MiddlewareHelper.DetermineHttpQueryStatusCode(executionResult);
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
