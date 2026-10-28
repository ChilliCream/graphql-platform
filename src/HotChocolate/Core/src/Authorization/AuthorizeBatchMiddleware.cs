using System.Collections.Immutable;
using HotChocolate.Resolvers;

namespace HotChocolate.Authorization;

internal sealed class AuthorizeBatchMiddleware(
    BatchFieldDelegate next,
    AuthorizeDirective directive)
{
    private readonly BatchFieldDelegate _next = next ??
        throw new ArgumentNullException(nameof(next));
    private readonly AuthorizeDirective _directive = directive ??
        throw new ArgumentNullException(nameof(directive));

    public async ValueTask InvokeAsync(ImmutableArray<IMiddlewareContext> contexts)
    {
        switch (_directive.Apply)
        {
            case ApplyPolicy.AfterResolver:
            {
                await _next(contexts).ConfigureAwait(false);

                foreach (var context in contexts)
                {
                    if (context.Result is null)
                    {
                        continue;
                    }

                    var handler = context.GetAuthorizationHandler();
                    var state = await handler.AuthorizeAsync(context, _directive).ConfigureAwait(false);

                    if (state != AuthorizeResult.Allowed && !AuthorizeMiddleware.IsErrorResult(context))
                    {
                        AuthorizeMiddleware.SetError(context, _directive, state);
                    }
                }
                break;
            }

            case ApplyPolicy.BeforeResolver:
            {
                // the authorization result can depend on the parent object, so each context
                // is authorized on its own and only the allowed ones are passed on to the resolver.
                ImmutableArray<IMiddlewareContext>.Builder? allowed = null;

                for (var i = 0; i < contexts.Length; i++)
                {
                    var context = contexts[i];
                    var handler = context.GetAuthorizationHandler();
                    var state = await handler.AuthorizeAsync(context, _directive).ConfigureAwait(false);

                    if (state == AuthorizeResult.Allowed)
                    {
                        allowed?.Add(context);
                        continue;
                    }

                    if (allowed is null)
                    {
                        allowed = ImmutableArray.CreateBuilder<IMiddlewareContext>(contexts.Length - 1);
                        allowed.AddRange(contexts, i);
                    }

                    AuthorizeMiddleware.SetError(context, _directive, state);
                }

                if (allowed is null)
                {
                    await _next(contexts).ConfigureAwait(false);
                }
                else if (allowed.Count > 0)
                {
                    await _next(allowed.ToImmutable()).ConfigureAwait(false);
                }
                break;
            }

            default:
                await _next(contexts).ConfigureAwait(false);
                break;
        }
    }
}
