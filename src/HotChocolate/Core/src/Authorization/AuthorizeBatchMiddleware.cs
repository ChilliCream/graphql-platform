using System.Collections.Immutable;
using HotChocolate.Resolvers;

namespace HotChocolate.Authorization;

internal sealed class AuthorizeBatchMiddleware(
    BatchFieldDelegate next,
    AuthorizeDirective directive)
{
    public async ValueTask InvokeAsync(ImmutableArray<IMiddlewareContext> contexts)
    {
        switch (directive.Apply)
        {
            case ApplyPolicy.AfterResolver:
                {
                    await next(contexts).ConfigureAwait(false);

                    foreach (var context in contexts)
                    {
                        if (context.Result is not null)
                        {
                            await AuthorizeAsync(context).ConfigureAwait(false);
                        }
                    }
                    break;
                }

            case ApplyPolicy.BeforeResolver:
                {
                    foreach (var context in contexts)
                    {
                        if (!context.HasErrors && context.Result is not IError and not IEnumerable<IError>)
                        {
                            await AuthorizeAsync(context).ConfigureAwait(false);
                        }
                    }

                    await next(contexts).ConfigureAwait(false);
                    break;
                }

            default:
                await next(contexts).ConfigureAwait(false);
                break;
        }
    }

    private async ValueTask AuthorizeAsync(IMiddlewareContext context)
    {
        try
        {
            var handler = context.GetAuthorizationHandler();
            var state = await handler.AuthorizeAsync(context, directive).ConfigureAwait(false);

            if (state != AuthorizeResult.Allowed
                && context.Result is not IError and not IEnumerable<IError>)
            {
                context.Result = ErrorHelper.NotAuthorized(context, directive, state);
            }
        }
        catch (Exception ex)
        {
            if (context.Result is IError or IEnumerable<IError>)
            {
                return;
            }

            if (!context.HasErrors && !context.RequestAborted.IsCancellationRequested)
            {
                context.ReportError(ex);
            }

            context.Result = null;
        }
    }
}
