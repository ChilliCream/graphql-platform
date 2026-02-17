#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Net;
using HotChocolate.AspNetCore.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

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
        var optionsHolder = new OptionsHolder();
        var executorProxy = HttpRequestExecutorProxy.Create(services, schemaName);

        groupBuilder.MapGet(
            "/{operationId}",
            async context =>
            {
                var options = optionsHolder.GetOptions(context);
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
                    options,
                    operationId,
                    operationName: null,
                    requireOperationName);
            });

        groupBuilder
            .MapGet(
                "/{operationId}/{operationName}",
                async context =>
                {
                    var options = optionsHolder.GetOptions(context);
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
                        options,
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

                    await ExecutePostRequestAsync(
                        context,
                        executorProxy,
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

                    await ExecutePostRequestAsync(
                        context,
                        executorProxy,
                        operationId,
                        operationName,
                        requireOperationName);
                });
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
            statusCode = parserResult.StatusCode.Value;
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

    private static async Task ExecutePostRequestAsync(
        HttpContext context,
        HttpRequestExecutorProxy executorProxy,
        string operationId,
        string? operationName,
        bool requireOperationName)
    {
        HttpStatusCode? statusCode;
        IExecutionResult? result;
        var ct = context.RequestAborted;
        var executorSession = await executorProxy.GetOrCreateSessionAsync(ct);

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
            statusCode = parserResult.StatusCode.Value;
            result = parserResult.Error;
            goto HANDLE_RESULT;
        }

        // after successfully parsing the request, we now will attempt to execute the request.
        var executionResult =
            await MiddlewareHelper.ExecuteRequestAsync(
                parserResult.Request!,
                validationResult.RequestFlags,
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

    private sealed class OptionsHolder
    {
        private GraphQLServerOptions? _options;

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
