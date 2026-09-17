using HotChocolate.Resolvers;

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
        var handler = context.GetAuthorizationHandler();

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
        => context.Result = ErrorHelper.NotAuthorized(context, _directive, state);
}
