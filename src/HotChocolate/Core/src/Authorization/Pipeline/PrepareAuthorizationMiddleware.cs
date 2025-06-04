using HotChocolate.Execution;

namespace HotChocolate.Authorization.Pipeline;

internal sealed class PrepareAuthorizationMiddleware(RequestDelegate next)
{
    public ValueTask InvokeAsync(RequestContext context)
    {
        context.EnsureAuthorizationRequestDataExists();
        return next(context);
    }

    public static RequestCoreMiddlewareConfiguration Create()
        => new RequestCoreMiddlewareConfiguration(
            static (_, next) =>
            {
                var middleware = new PrepareAuthorizationMiddleware(next);
                return middleware.InvokeAsync;
            },
            "HotChocolate.Authorization.Pipeline.PrepareAuthorization");
}
