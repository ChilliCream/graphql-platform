using HotChocolate.Resolvers;
using static HotChocolate.Authorization.Properties.AuthCoreResources;

namespace HotChocolate.Authorization;

internal static class ErrorHelper
{
    public static IError NotAuthorized(
        IMiddlewareContext context,
        AuthorizeDirective directive,
        AuthorizeResult state)
        => state switch
        {
            AuthorizeResult.NoDefaultPolicy
                => ErrorBuilder.New()
                    .SetMessage(AuthorizeMiddleware_NoDefaultPolicy)
                    .SetCode(ErrorCodes.Authentication.NoDefaultPolicy)
                    .SetPath(context.Path)
                    .AddLocations(context.Selection)
                    .Build(),
            AuthorizeResult.PolicyNotFound
                => ErrorBuilder.New()
                    .SetMessage(AuthorizeMiddleware_PolicyNotFound, directive.Policy)
                    .SetCode(ErrorCodes.Authentication.PolicyNotFound)
                    .SetPath(context.Path)
                    .AddLocations(context.Selection)
                    .Build(),
            _
                => ErrorBuilder.New()
                    .SetMessage(AuthorizeMiddleware_NotAuthorized)
                    .SetCode(
                        state == AuthorizeResult.NotAllowed
                            ? ErrorCodes.Authentication.NotAuthorized
                            : ErrorCodes.Authentication.NotAuthenticated)
                    .SetPath(context.Path)
                    .AddLocations(context.Selection)
                    .Build()
        };
}
