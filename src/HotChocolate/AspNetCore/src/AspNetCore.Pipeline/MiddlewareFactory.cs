using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

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
        HttpRequestExecutorProxy executor,
        GraphQLServerOptions serverOptions)
    {
        return next =>
        {
            var middleware = new WebSocketSubscriptionMiddleware(next, executor, serverOptions);
            return context => middleware.InvokeAsync(context);
        };
    }

    internal static Func<RequestDelegate, RequestDelegate> CreateHttpPostMiddleware(
        HttpRequestExecutorProxy executor,
        GraphQLServerOptions serverOptions)
    {
        return next =>
        {
            var middleware = new HttpPostMiddleware(next, executor, serverOptions);
            return context => middleware.InvokeAsync(context);
        };
    }

    internal static Func<RequestDelegate, RequestDelegate> CreateHttpMultipartMiddleware(
        HttpRequestExecutorProxy executor,
        GraphQLServerOptions serverOptions,
        IOptions<FormOptions> formOptions)
    {
        return next =>
        {
            var middleware = new HttpMultipartMiddleware(next, executor, serverOptions, formOptions);
            return context => middleware.InvokeAsync(context);
        };
    }

    internal static Func<RequestDelegate, RequestDelegate> CreateHttpGetMiddleware(
        HttpRequestExecutorProxy executor,
        GraphQLServerOptions serverOptions)
    {
        return next =>
        {
            var middleware = new HttpGetMiddleware(next, executor, serverOptions);
            return context => middleware.InvokeAsync(context);
        };
    }

    internal static Func<RequestDelegate, RequestDelegate> CreateHttpGetSchemaMiddleware(
        HttpRequestExecutorProxy executor,
        GraphQLServerOptions serverOptions,
        PathString path,
        MiddlewareRoutingType routingType)
    {
        return next =>
        {
            var middleware = new HttpGetSchemaMiddleware(next, executor, serverOptions, path, routingType);
            return context => middleware.InvokeAsync(context);
        };
    }

    internal static Func<RequestDelegate, RequestDelegate> CreateHttpGetSemanticNonNullSchemaMiddleware(
        HttpRequestExecutorProxy executor,
        GraphQLServerOptions serverOptions)
    {
        return next =>
        {
            var middleware = new HttpGetSemanticNonNullSchemaMiddleware(next, executor, serverOptions);
            return context => middleware.InvokeAsync(context);
        };
    }
}
