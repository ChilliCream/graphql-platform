namespace HotChocolate.Execution.Pipeline;

internal sealed class SkipWarmupExecutionMiddleware(RequestDelegate next)
{
    public ValueTask InvokeAsync(RequestContext context)
    {
        if (context.IsWarmupRequest())
        {
            context.Result = new WarmupExecutionResult();
            return default;
        }

        return next(context);
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (_, next) =>
            {
                var middleware = new SkipWarmupExecutionMiddleware(next);
                return context => middleware.InvokeAsync(context);
            },
            WellKnownRequestMiddleware.SkipWarmupExecutionMiddleware);
}
