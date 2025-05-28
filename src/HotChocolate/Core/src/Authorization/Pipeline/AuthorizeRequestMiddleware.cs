using HotChocolate.Authorization.Properties;
using HotChocolate.Execution;

namespace HotChocolate.Authorization.Pipeline;

internal sealed class AuthorizeRequestMiddleware(
    RequestDelegate next,
    IServiceProvider applicationServices)
{
    public async ValueTask InvokeAsync(IRequestContext context)
    {
        if (context.Document is null || context.DocumentId is null)
        {
            throw new InvalidOperationException(
                "The document or document id is not set.");
        }

        var directives = context.GetAuthorizeDirectives();

        if (directives.Length > 0)
        {
            var handler = context.GetAuthorizationHandler();

            var authorizationContext = new AuthorizationContext(
                context.Schema,
                applicationServices,
                context.ContextData,
                context.Features,
                context.Document,
                context.DocumentId.Value);

            var result = await handler.AuthorizeAsync(
                authorizationContext,
                directives,
                context.RequestAborted)
                .ConfigureAwait(false);

            if (result is not AuthorizeResult.Allowed)
            {
                context.Result = CreateErrorResult(result);
                return;
            }
        }

        await next(context);
    }

    private static IOperationResult CreateErrorResult(AuthorizeResult result)
        => OperationResultBuilder.New()
            .AddError(CreateError(result))
            .SetContextData(WellKnownContextData.HttpStatusCode, 401)
            .Build();

    private static IError CreateError(AuthorizeResult result)
    {
        return result switch
        {
            AuthorizeResult.NoDefaultPolicy
                => ErrorBuilder.New()
                    .SetMessage(AuthCoreResources.AuthorizeMiddleware_NoDefaultPolicy)
                    .SetCode(ErrorCodes.Authentication.NoDefaultPolicy)
                    .Build(),

            AuthorizeResult.PolicyNotFound
                => ErrorBuilder.New()
                    .SetMessage(AuthCoreResources.AuthorizeMiddleware_PoliciesMissing)
                    .SetCode(ErrorCodes.Authentication.PolicyNotFound)
                    .Build(),
            _
                => ErrorBuilder.New()
                    .SetMessage(AuthCoreResources.AuthorizeMiddleware_NotAuthorized)
                    .SetCode(result == AuthorizeResult.NotAllowed
                        ? ErrorCodes.Authentication.NotAuthorized
                        : ErrorCodes.Authentication.NotAuthenticated)
                    .Build(),
        };
    }

    public static RequestCoreMiddlewareConfiguration Create()
        => new RequestCoreMiddlewareConfiguration(
            static (context, next) =>
            {
                var middleware = new AuthorizeRequestMiddleware(next, context.Services);
                return middleware.InvokeAsync;
            },
            "HotChocolate.Authorization.Pipeline.AuthorizeRequest");
}
