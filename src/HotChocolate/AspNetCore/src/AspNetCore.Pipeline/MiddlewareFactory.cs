#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal static class MiddlewareFactory
{
    internal static Func<RequestDelegate, RequestDelegate> CreateCancellationMiddleware()
    {
        return next => async context =>
        {
            try
            {
                await next(context);
            }
            catch (OperationCanceledException)
            {
                // we just catch cancellations here and do nothing.
            }
        };
    }

    internal static Func<RequestDelegate, RequestDelegate> CreateWebSocketSubscriptionMiddleware(
        HttpRequestExecutorProxy executor)
    {
        return next =>
        {
            var middleware = new WebSocketSubscriptionMiddleware(next, executor);
            return context => middleware.InvokeAsync(context);
        };
    }

    internal static Func<RequestDelegate, RequestDelegate> CreateHttpPostMiddleware(
        HttpRequestExecutorProxy executor)
    {
        return next =>
        {
            var middleware = new HttpPostMiddleware(next, executor);
            return context => middleware.InvokeAsync(context);
        };
    }

    internal static Func<RequestDelegate, RequestDelegate> CreateHttpMultipartMiddleware(
        HttpRequestExecutorProxy executor,
        IOptions<FormOptions> formOptions)
    {
        return next =>
        {
            var middleware = new HttpMultipartMiddleware(next, executor, formOptions);
            return context => middleware.InvokeAsync(context);
        };
    }

    internal static Func<RequestDelegate, RequestDelegate> CreateHttpGetMiddleware(
        HttpRequestExecutorProxy executor)
    {
        return next =>
        {
            var middleware = new HttpGetMiddleware(next, executor);
            return context => middleware.InvokeAsync(context);
        };
    }

    internal static Func<RequestDelegate, RequestDelegate> CreateHttpGetSchemaMiddleware(
        HttpRequestExecutorProxy executor,
        PathString path,
        MiddlewareRoutingType routingType)
    {
        return next =>
        {
            var middleware = new HttpGetSchemaMiddleware(next, executor, path, routingType);
            return context => middleware.InvokeAsync(context);
        };
    }
}
