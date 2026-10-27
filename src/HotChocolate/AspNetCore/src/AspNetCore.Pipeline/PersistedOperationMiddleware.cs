using System.Diagnostics.CodeAnalysis;
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
    // See https://github.com/dotnet/aspnetcore/issues/69972
    [SuppressMessage(
        "Usage",
        "ASP0022:Route conflict detected between route handlers",
        Justification =
            "Routing prefers an endpoint with HTTP method metadata over a method-agnostic "
            + "endpoint on the same route pattern.")]
    internal static void MapPersistedOperationMiddleware(
        this RouteGroupBuilder groupBuilder,
        IServiceProvider services,
        string schemaName,
        bool requireOperationName)
    {
        var executorProxy = HttpRequestExecutorProxy.Create(services, schemaName);
        var serverOptions = services.GetRequiredService<IOptionsMonitor<GraphQLServerOptions>>().Get(schemaName);
        var negotiation = MiddlewareFactory.CreateHttpContentNegotiationMiddleware(path: null);

        ((IEndpointConventionBuilder)groupBuilder).Add(
            endpoint => endpoint.RequestDelegate = negotiation(endpoint.RequestDelegate!));

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
                        serverOptions,
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
                        serverOptions,
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
                        serverOptions,
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
                        serverOptions,
                        HttpRequestKind.HttpQuery,
                        operationId,
                        operationName,
                        requireOperationName);
                });
        }

        var allow = serverOptions.EnableQueryRequests
            ? "GET, OPTIONS, POST, QUERY"
            : "GET, OPTIONS, POST";

        // every other method on these patterns is answered as on the GraphQL endpoint.
        groupBuilder.Map(
            "/{operationId}",
            context => HandleUnmappedMethodAsync(context, executorProxy, allow, serverOptions));

        groupBuilder.Map(
            "/{operationId}/{operationName}",
            context => HandleUnmappedMethodAsync(context, executorProxy, allow, serverOptions));
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
                    options,
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
        GraphQLServerOptions options,
        HttpRequestKind kind,
        string operationId,
        string? operationName,
        bool requireOperationName)
    {
        HttpStatusCode? statusCode;
        IExecutionResult? result;
        var ct = context.RequestAborted;
        var executorSession = await executorProxy.GetOrCreateSessionAsync(ct);

        // first, we refuse a request whose body is not JSON.
        if (context.ParseContentType() is not RequestContentType.Json)
        {
            if (executorSession.ReportsUnsupportedMethodOrMediaType)
            {
                context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                context.Response.Headers.SetAcceptQuery(options);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
            }

            return;
        }

        using (executorSession.DiagnosticEvents.ExecuteHttpRequest(context, kind))
        {
            // next, we validate the accept-headers.
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
                    options,
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

    private static async Task HandleUnmappedMethodAsync(
        HttpContext context,
        HttpRequestExecutorProxy executorProxy,
        string allow,
        GraphQLServerOptions options)
    {
        var executorSession = await executorProxy.GetOrCreateSessionAsync(context.RequestAborted);

        if (!executorSession.ReportsUnsupportedMethodOrMediaType)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // RFC 9110, section 9.3.7: OPTIONS asks for the methods the target resource supports,
        // and section 15.5.6 requires a 405 to list them.
        context.Response.StatusCode = HttpMethods.IsOptions(context.Request.Method)
            ? StatusCodes.Status204NoContent
            : StatusCodes.Status405MethodNotAllowed;
        context.Response.Headers.Allow = allow;
        context.Response.Headers.SetAcceptQuery(options);
    }
}
