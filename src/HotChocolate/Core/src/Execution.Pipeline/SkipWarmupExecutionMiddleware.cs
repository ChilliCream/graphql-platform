namespace HotChocolate.Execution.Pipeline;

internal sealed class SkipWarmupExecutionMiddleware(RequestDelegate next)
{
    public async ValueTask InvokeAsync(RequestContext context)
    {
        if (context.IsWarmupRequest())
        {
            context.Result = new WarmupExecutionResult();
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (_, next) =>
            {
                var middleware = new SkipWarmupExecutionMiddleware(next);
                return context => middleware.InvokeAsync(context);
            },
            nameof(SkipWarmupExecutionMiddleware));
}
