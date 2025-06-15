using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class OperationExecutionMiddleware
{
    public ValueTask InvokeAsync(
        RequestContext context,
        RequestDelegate next)
    {
        return next(context);
    }

    public static RequestMiddlewareConfiguration Create()
    {
        return new RequestMiddlewareConfiguration(
            (fc, next) =>
            {
                var middleware = new OperationExecutionMiddleware();
                return requestContext => middleware.InvokeAsync(requestContext, next);
            },
            nameof(OperationExecutionMiddleware));
    }
}