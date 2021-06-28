using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Authorization.Properties;
using HotChocolate.Resolvers;

namespace HotChocolate.AspNetCore.Authorization
{
    internal sealed class AuthorizeMiddleware
    {
        private readonly FieldDelegate _next;

        public AuthorizeMiddleware(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IDirectiveContext context)
        {
            AuthorizeDirective directive = context.Directive
                .ToObject<AuthorizeDirective>();

            if (directive.Apply == ApplyPolicy.AfterResolver)
            {
                await _next(context).ConfigureAwait(false);

                AuthState state = await CheckPermissionsAsync(
                    context, directive)
                    .ConfigureAwait(false);

                if (state != AuthState.Allowed && !IsErrorResult(context))
                {
                    SetError(context, directive, state);
                }
            }
            else
            {
                AuthState state = await CheckPermissionsAsync(
                    context, directive)
                    .ConfigureAwait(false);

                if (state == AuthState.Allowed)
                {
                    await _next(context).ConfigureAwait(false);
                }
                else
                {
                    SetError(context, directive, state);
                }
            }
        }

        private bool IsErrorResult(IDirectiveContext context) =>
            context.Result is IError || context.Result is IEnumerable<IError>;

        private void SetError(
            IDirectiveContext context,
            AuthorizeDirective directive,
            AuthState state)
        {
            switch (state)
            {
                case AuthState.NoDefaultPolicy:
                    context.Result = context.Result = ErrorBuilder.New()
                        .SetMessage(AuthResources.AuthorizeMiddleware_NoDefaultPolicy)
                        .SetCode(ErrorCodes.Authentication.NoDefaultPolicy)
                        .SetPath(context.Path)
                        .AddLocation(context.Selection.SyntaxNode)
                        .Build();
                    break;

                case AuthState.PolicyNotFound:
                    context.Result = ErrorBuilder.New()
                        .SetMessage(string.Format(
                            CultureInfo.InvariantCulture,
                            AuthResources.AuthorizeMiddleware_PolicyNotFound,
                            directive.Policy))
                        .SetCode(ErrorCodes.Authentication.PolicyNotFound)
                        .SetPath(context.Path)
                        .AddLocation(context.Selection.SyntaxNode)
                        .Build();
                    break;

                default:
                    context.Result = ErrorBuilder.New()
                        .SetMessage(AuthResources.AuthorizeMiddleware_NotAuthorized)
                        .SetCode(state == AuthState.NotAllowed
                            ? ErrorCodes.Authentication.NotAuthorized
                            : ErrorCodes.Authentication.NotAuthenticated)
                        .SetPath(context.Path)
                        .AddLocation(context.Selection.SyntaxNode)
                        .Build();
                    break;
            }
        }

        private static async Task<AuthState> CheckPermissionsAsync(
            IDirectiveContext context,
            AuthorizeDirective directive)
        {
            if (!TryGetAuthenticatedPrincipal(context, out ClaimsPrincipal? principal))
            {
                return AuthState.NotAuthenticated;
            }

            if (IsInAnyRole(principal!, directive.Roles))
            {
                if (NeedsPolicyValidation(directive))
                {
                    return await AuthorizeWithPolicyAsync(
                        context, directive, principal!)
                        .ConfigureAwait(false);
                }
                else
                {
                    return AuthState.Allowed;
                }
            }

            return AuthState.NotAllowed;
        }

#if NETCOREAPP2_2 || NETCOREAPP2_1 || NETSTANDARD2_0 ||  NET462
        private static bool TryGetAuthenticatedPrincipal(
            IDirectiveContext context,
            out ClaimsPrincipal? principal)
#else
        private static bool TryGetAuthenticatedPrincipal(
            IDirectiveContext context,
            [NotNullWhen(true)]out ClaimsPrincipal? principal)
#endif
        {
            if (context.ContextData.TryGetValue(nameof(ClaimsPrincipal), out var o)
                && o is ClaimsPrincipal p
                && p.Identities.Any(t => t.IsAuthenticated))
            {
                principal = p;
                return true;
            }

            principal = null;
            return false;
        }

        private static bool IsInAnyRole(
            IPrincipal principal,
            IReadOnlyList<string>? roles)
        {
            if (roles == null || roles.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < roles.Count; i++)
            {
                if (principal.IsInRole(roles[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool NeedsPolicyValidation(AuthorizeDirective directive)
        {
            return directive.Roles == null
                || directive.Roles.Count == 0
                || !string.IsNullOrEmpty(directive.Policy);
        }

        private static async Task<AuthState> AuthorizeWithPolicyAsync(
            IDirectiveContext context,
            AuthorizeDirective directive,
            ClaimsPrincipal principal)
        {
            IServiceProvider services = context.Service<IServiceProvider>();
            IAuthorizationService? authorizeService =
                services.GetService<IAuthorizationService>();
            IAuthorizationPolicyProvider? policyProvider =
                services.GetService<IAuthorizationPolicyProvider>();

            if (authorizeService == null || policyProvider == null)
            {
                // authorization service is not configured so the user is
                // authorized with the previous checks.
                return string.IsNullOrWhiteSpace(directive.Policy)
                    ? AuthState.Allowed
                    : AuthState.NotAllowed;
            }

            AuthorizationPolicy? policy = null;

            if ((directive.Roles is null || directive.Roles.Count == 0)
                && string.IsNullOrWhiteSpace(directive.Policy))
            {
                policy = await policyProvider.GetDefaultPolicyAsync()
                    .ConfigureAwait(false);

                if (policy == null)
                {
                    return AuthState.NoDefaultPolicy;
                }
            }
            else if (!string.IsNullOrWhiteSpace(directive.Policy))
            {
                policy = await policyProvider.GetPolicyAsync(directive.Policy)
                    .ConfigureAwait(false);

                if (policy == null)
                {
                    return AuthState.PolicyNotFound;
                }
            }

            if (policy is not null)
            {
                AuthorizationResult result =
                    await authorizeService.AuthorizeAsync(
                        principal, context, policy)
                        .ConfigureAwait(false);
                return result.Succeeded ? AuthState.Allowed : AuthState.NotAllowed;
            }

            return AuthState.NotAllowed;
        }

        private enum AuthState
        {
            Allowed,
            NotAllowed,
            NotAuthenticated,
            NoDefaultPolicy,
            PolicyNotFound
        }
    }
}
