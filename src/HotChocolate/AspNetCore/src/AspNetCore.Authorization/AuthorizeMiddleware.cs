using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Authorization.Properties;
using HotChocolate.Resolvers;

namespace HotChocolate.AspNetCore.Authorization;

internal sealed class AuthorizeMiddleware
{
    private readonly FieldDelegate _next;
    private readonly IAuthorizationHandler _authorizationHandler;

    public AuthorizeMiddleware(FieldDelegate next, IAuthorizationHandler authorizationHandler)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _authorizationHandler = authorizationHandler ??
            throw new ArgumentNullException(nameof(authorizationHandler));
    }

    public async Task InvokeAsync(IDirectiveContext context)
    {
        AuthorizeDirective directive = context.Directive
            .AsValue<AuthorizeDirective>();

        if (directive.Apply == ApplyPolicy.AfterResolver)
        {
            await _next(context).ConfigureAwait(false);

            AuthorizeResult state =
                await _authorizationHandler.AuthorizeAsync(context, directive)
                    .ConfigureAwait(false);

            if (state != AuthorizeResult.Allowed && !IsErrorResult(context))
            {
                SetError(context, directive, state);
            }
        }
        else
        {
            AuthorizeResult state =
                await _authorizationHandler.AuthorizeAsync(context, directive)
                    .ConfigureAwait(false);

            if (state == AuthorizeResult.Allowed)
            {
                await _next(context).ConfigureAwait(false);
            }
            else
            {
                SetError(context, directive, state);
            }
        }
    }

    private bool IsErrorResult(IMiddlewareContext context)
        => context.Result is IError or IEnumerable<IError>;

    private void SetError(
        IMiddlewareContext context,
        AuthorizeDirective directive,
        AuthorizeResult state)
        => context.Result = state switch
        {
            AuthorizeResult.NoDefaultPolicy
                => ErrorBuilder.New()
                    .SetMessage(AuthResources.AuthorizeMiddleware_NoDefaultPolicy)
                    .SetCode(ErrorCodes.Authentication.NoDefaultPolicy)
                    .SetPath(context.Path)
                    .AddLocation(context.Selection.SyntaxNode)
                    .Build(),
            AuthorizeResult.PolicyNotFound
                => ErrorBuilder.New()
                    .SetMessage(
                        AuthResources.AuthorizeMiddleware_PolicyNotFound,
                        directive.Policy!)
                    .SetCode(ErrorCodes.Authentication.PolicyNotFound)
                    .SetPath(context.Path)
                    .AddLocation(context.Selection.SyntaxNode)
                    .Build(),
            _
                => ErrorBuilder.New()
                    .SetMessage(AuthResources.AuthorizeMiddleware_NotAuthorized)
                    .SetCode(state == AuthorizeResult.NotAllowed
                        ? ErrorCodes.Authentication.NotAuthorized
                        : ErrorCodes.Authentication.NotAuthenticated)
                    .SetPath(context.Path)
                    .AddLocation(context.Selection.SyntaxNode)
                    .Build()
        };
}
