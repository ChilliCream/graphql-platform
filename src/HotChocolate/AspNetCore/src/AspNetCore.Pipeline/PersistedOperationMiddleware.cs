#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Net;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HotChocolate.AspNetCore;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal static class PersistedOperationMiddleware
{
    internal static void MapPersistedOperationMiddleware(
        this RouteGroupBuilder groupBuilder,
        IServiceProvider services,
        string schemaName,
        bool requireOperationName)
    {
        var executorProxy = HttpRequestExecutorProxy.Create(services, schemaName);
        var serverOptions = services.GetRequiredService<IOptionsMonitor<GraphQLServerOptions>>().Get(schemaName);

        groupBuilder.MapGet(
            "/{operationId}",
            async context =>
            {
                var operationId = context.Request.RouteValues["operationId"] as string;

                if (string.IsNullOrEmpty(operationId))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Missing operationId", context.RequestAborted);
                    return;
                }

                await ExecuteGetRequestAsync(
                    context,
                    executorProxy,
                    serverOptions,
                    operationId,
                    operationName: null,
                    requireOperationName);
            });

        groupBuilder
            .MapGet(
                "/{operationId}/{operationName}",
                async context =>
                {
                    var operationId = context.Request.RouteValues["operationId"] as string;
                    var operationName = context.Request.RouteValues["operationName"] as string;

                    if (string.IsNullOrEmpty(operationId))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Missing operationId", context.RequestAborted);
                        return;
                    }

                    if (string.IsNullOrEmpty(operationName))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Missing operationName", context.RequestAborted);
                        return;
                    }

                    await ExecuteGetRequestAsync(
                        context,
                        executorProxy,
                        serverOptions,
                        operationId,
                        operationName,
                        requireOperationName);
                });

        groupBuilder
            .MapPost(
                "/{operationId}",
                async context =>
                {
                    var operationId = context.Request.RouteValues["operationId"] as string;

                    if (string.IsNullOrEmpty(operationId))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Missing operationId", context.RequestAborted);
                        return;
                    }

                    await ExecuteBodyRequestAsync(
                        context,
                        executorProxy,
                        HttpRequestKind.HttpPost,
                        operationId,
                        operationName: null,
                        requireOperationName);
                });

        groupBuilder
            .MapPost(
                "/{operationId}/{operationName}",
                async context =>
                {
                    var operationId = context.Request.RouteValues["operationId"] as string;
                    var operationName = context.Request.RouteValues["operationName"] as string;

                    if (string.IsNullOrEmpty(operationId))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Missing operationId", context.RequestAborted);
                        return;
                    }

                    if (string.IsNullOrEmpty(operationName))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Missing operationName", context.RequestAborted);
                        return;
                    }

                    await ExecuteBodyRequestAsync(
                        context,
                        executorProxy,
                        HttpRequestKind.HttpPost,
                        operationId,
                        operationName,
                        requireOperationName);
                });

        if (serverOptions.EnableQueryRequests)
        {
            groupBuilder.MapMethods(
                "/{operationId}",
                [HttpMethods.Query],
                async context =>
                {
                    var operationId = context.Request.RouteValues["operationId"] as string;

                    if (string.IsNullOrEmpty(operationId))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync(
                            "Missing operationId",
                            context.RequestAborted);
                        return;
                    }

                    await ExecuteBodyRequestAsync(
                        context,
                        executorProxy,
                        HttpRequestKind.HttpQuery,
                        operationId,
                        operationName: null,
                        requireOperationName);
                });

            groupBuilder.MapMethods(
                "/{operationId}/{operationName}",
                [HttpMethods.Query],
                async context =>
                {
                    var operationId = context.Request.RouteValues["operationId"] as string;
                    var operationName = context.Request.RouteValues["operationName"] as string;

                    if (string.IsNullOrEmpty(operationId))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync(
                            "Missing operationId",
                            context.RequestAborted);
                        return;
                    }

                    if (string.IsNullOrEmpty(operationName))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync(
                            "Missing operationName",
                            context.RequestAborted);
                        return;
                    }

                    await ExecuteBodyRequestAsync(
                        context,
                        executorProxy,
                        HttpRequestKind.HttpQuery,
                        operationId,
                        operationName,
                        requireOperationName);
                });
        }
    }

    private static async Task ExecuteGetRequestAsync(
        HttpContext context,
        HttpRequestExecutorProxy executorProxy,
        GraphQLServerOptions options,
        string operationId,
        string? operationName,
        bool requireOperationName)
    {
        HttpStatusCode? statusCode;
        IExecutionResult? result;
        var ct = context.RequestAborted;
        var executorSession = await executorProxy.GetOrCreateSessionAsync(ct);

        using (executorSession.DiagnosticEvents.ExecuteHttpRequest(context, HttpRequestKind.HttpGet))
        {
            // first, we validate the accept-headers.
            var validationResult = MiddlewareHelper.ValidateAcceptContentType(context, executorSession);
            var acceptMediaTypes = validationResult.AcceptMediaTypes;

            if (!validationResult.IsValid)
            {
                statusCode = validationResult.StatusCode.Value;
                result = validationResult.Error;
                goto HANDLE_RESULT;
            }

            // validate if the operation name is required.
            if (requireOperationName && string.IsNullOrWhiteSpace(operationName))
            {
                statusCode = HttpStatusCode.BadRequest;
                result = ErrorHelper.OperationNameRequired();
                goto HANDLE_RESULT;
            }

            // next, we parse the GraphQL request.
            var parserResult =
                MiddlewareHelper.ParseVariablesAndExtensionsFromParams(
                    operationId,
                    operationName,
                    context,
                    executorSession);

            if (!parserResult.IsValid)
            {
                statusCode = parserResult.StatusCode;
                result = parserResult.Error;
                goto HANDLE_RESULT;
            }

            // before we can execute the request, we need to determine the request flags.
            var request = parserResult.Request!;
            var requestFlags =
                MiddlewareHelper.DetermineHttpGetRequestFlags(
                    validationResult.RequestFlags,
                    options);

            // next, we will execute the request.
            var executionResult =
                await MiddlewareHelper.ExecuteRequestAsync(
                    request,
                    requestFlags,
                    context,
                    executorSession);
            statusCode = executionResult.StatusCode;
            result = executionResult.Result;

HANDLE_RESULT:
            await MiddlewareHelper.WriteResultAsync(
                result!,
                acceptMediaTypes,
                statusCode,
                context,
                executorSession);
        }
    }

    private static async Task ExecuteBodyRequestAsync(
        HttpContext context,
        HttpRequestExecutorProxy executorProxy,
        HttpRequestKind kind,
        string operationId,
        string? operationName,
        bool requireOperationName)
    {
        HttpStatusCode? statusCode;
        IExecutionResult? result;
        var ct = context.RequestAborted;
        var executorSession = await executorProxy.GetOrCreateSessionAsync(ct);

        using (executorSession.DiagnosticEvents.ExecuteHttpRequest(context, kind))
        {
            // first, we validate the accept-headers.
            var validationResult = MiddlewareHelper.ValidateAcceptContentType(context, executorSession);

            var acceptMediaTypes = validationResult.AcceptMediaTypes;

            if (!validationResult.IsValid)
            {
                statusCode = validationResult.StatusCode.Value;
                result = validationResult.Error;
                goto HANDLE_RESULT;
            }

            // validate if the operation name is required.
            if (requireOperationName && string.IsNullOrWhiteSpace(operationName))
            {
                statusCode = HttpStatusCode.BadRequest;
                result = ErrorHelper.OperationNameRequired();
                goto HANDLE_RESULT;
            }

            // next, we parse the GraphQL request.
            var parserResult =
                await MiddlewareHelper.ParseSingleRequestFromBodyAsync(
                    operationId,
                    operationName,
                    context,
                    executorSession);

            if (!parserResult.IsValid)
            {
                statusCode = parserResult.StatusCode;
                result = parserResult.Error;
                goto HANDLE_RESULT;
            }

            var request = parserResult.Request!;
            var requestFlags = validationResult.RequestFlags;

            if (kind is HttpRequestKind.HttpQuery)
            {
                // a QUERY request carries exactly one variable set, so a variable batch is
                // refused.
                if (MiddlewareHelper.IsVariableBatch(request))
                {
                    var refused = MiddlewareHelper.CreateBatchingRefusedResult(
                        ErrorHelper.VariableBatchingNotSupportedForQuery(),
                        context,
                        executorSession);
                    statusCode = refused.StatusCode;
                    result = refused.Error;
                    goto HANDLE_RESULT;
                }

                requestFlags = MiddlewareHelper.DetermineHttpQueryRequestFlags(requestFlags);
            }

            // after successfully parsing the request, we now will attempt to execute the request.
            var executionResult =
                await MiddlewareHelper.ExecuteRequestAsync(
                    request,
                    requestFlags,
                    context,
                    executorSession);
            statusCode = kind is HttpRequestKind.HttpQuery
                ? MiddlewareHelper.DetermineHttpQueryStatusCode(executionResult)
                : executionResult.StatusCode;
            result = executionResult.Result;

HANDLE_RESULT:
            await MiddlewareHelper.WriteResultAsync(
                result!,
                acceptMediaTypes,
                statusCode,
                context,
                executorSession);
        }
    }
}
