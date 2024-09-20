using HotChocolate.Resolvers;
using static HotChocolate.Authorization.Properties.AuthCoreResources;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Authorization;

internal sealed class AuthorizeMiddleware(
    FieldDelegate next,
    AuthorizeDirective directive)
{
    private readonly FieldDelegate _next = next ??
        throw new ArgumentNullException(nameof(next));
    private readonly AuthorizeDirective _directive = directive ??
        throw new ArgumentNullException(nameof(directive));

    public async Task InvokeAsync(IMiddlewareContext context)
    {
        var handler = context.GetGlobalStateOrDefault<IAuthorizationHandler>(AuthorizationHandler);

        if (handler is null)
        {
            throw new MissingStateException(
                "Authorization",
                AuthorizationHandler,
                StateKind.Global);
        }

        switch (_directive.Apply)
        {
            case ApplyPolicy.AfterResolver:
            {
                await _next(context).ConfigureAwait(false);

                if (context.Result is not null)
                {
                    var state = await handler.AuthorizeAsync(context, _directive)
                        .ConfigureAwait(false);

                    if (state != AuthorizeResult.Allowed && !IsErrorResult(context))
                    {
                        SetError(context, state);
                    }
                }
                break;
            }

            case ApplyPolicy.BeforeResolver:
            {
                var state = await handler.AuthorizeAsync(context, _directive).ConfigureAwait(false);

                if (state == AuthorizeResult.Allowed)
                {
                    await _next(context).ConfigureAwait(false);
                }
                else
                {
                    SetError(context, state);
                }
                break;
            }

            default:
                await _next(context).ConfigureAwait(false);
                break;
        }
    }

    private static bool IsErrorResult(IMiddlewareContext context)
        => context.Result is IError or IEnumerable<IError>;

    private void SetError(
        IMiddlewareContext context,
        AuthorizeResult state)
        => context.Result = state switch
        {
            AuthorizeResult.NoDefaultPolicy
                => ErrorBuilder.New()
                    .SetMessage(AuthorizeMiddleware_NoDefaultPolicy)
                    .SetCode(ErrorCodes.Authentication.NoDefaultPolicy)
                    .SetPath(context.Path)
                    .SetLocations([context.Selection.SyntaxNode])
                    .Build(),
            AuthorizeResult.PolicyNotFound
                => ErrorBuilder.New()
                    .SetMessage(
                        AuthorizeMiddleware_PolicyNotFound,
                        _directive.Policy!)
                    .SetCode(ErrorCodes.Authentication.PolicyNotFound)
                    .SetPath(context.Path)
                    .SetLocations([context.Selection.SyntaxNode])
                    .Build(),
            _
                => ErrorBuilder.New()
                    .SetMessage(AuthorizeMiddleware_NotAuthorized)
                    .SetCode(
                        state == AuthorizeResult.NotAllowed
                            ? ErrorCodes.Authentication.NotAuthorized
                            : ErrorCodes.Authentication.NotAuthenticated)
                    .SetPath(context.Path)
                    .SetLocations([context.Selection.SyntaxNode])
                    .Build(),
        };
}
