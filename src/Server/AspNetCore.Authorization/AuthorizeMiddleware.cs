using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Authorization.Properties;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
#if ASPNETCLASSIC

namespace HotChocolate.AspNetClassic.Authorization
#else
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Authorization
#endif
{
    internal class AuthorizeMiddleware
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

            ClaimsPrincipal principal = null;
            var allowed = false;
            var authenticated = false;

            if (context.ContextData.TryGetValue(
                nameof(ClaimsPrincipal), out var o)
                && o is ClaimsPrincipal p)
            {
                principal = p;
                authenticated = allowed =
                    p.Identities.Any(t => t.IsAuthenticated);
            }

            allowed = allowed && IsInAnyRole(principal, directive.Roles);

#if !ASPNETCLASSIC
            if (allowed && NeedsPolicyValidation(directive))
            {
                allowed = await AuthorizeWithPolicyAsync(
                    context, directive, principal)
                    .ConfigureAwait(false);
            }
#endif
            if (allowed)
            {
                await _next(context).ConfigureAwait(false);
            }
            else if (context.Result == null)
            {
                context.Result = ErrorBuilder.New()
                    .SetMessage(
                        AuthResources.AuthorizeMiddleware_NotAuthorized)
                    .SetCode(authenticated
                        ? AuthErrorCodes.NotAuthorized
                        : AuthErrorCodes.NotAuthenticated)
                    .SetPath(context.Path)
                    .AddLocation(context.FieldSelection)
                    .Build();
            }
        }

        private static bool IsInAnyRole(
            IPrincipal principal,
            IReadOnlyList<string> roles)
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
#if !ASPNETCLASSIC

        private static bool NeedsPolicyValidation(
            AuthorizeDirective directive)
        {
            return directive.Roles.Count == 0
                || !string.IsNullOrEmpty(directive.Policy);
        }

        private static async Task<bool> AuthorizeWithPolicyAsync(
            IDirectiveContext context,
            AuthorizeDirective directive,
            ClaimsPrincipal principal)
        {
            IServiceProvider services = context.Service<IServiceProvider>();
            IAuthorizationService authorizeService =
                services.GetService<IAuthorizationService>();
            IAuthorizationPolicyProvider policyProvider =
                services.GetService<IAuthorizationPolicyProvider>();

            if (authorizeService == null || policyProvider == null)
            {
                return string.IsNullOrWhiteSpace(directive.Policy);
            }

            AuthorizationPolicy policy = null;

            if (directive.Roles.Count == 0
                && string.IsNullOrWhiteSpace(directive.Policy))
            {
                policy = await policyProvider.GetDefaultPolicyAsync()
                    .ConfigureAwait(false);

                if (policy == null)
                {
                    context.Result = context.Result = ErrorBuilder.New()
                        .SetMessage(
                            AuthResources.AuthorizeMiddleware_NoDefaultPolicy)
                        .SetCode(AuthErrorCodes.NoDefaultPolicy)
                        .SetPath(context.Path)
                        .AddLocation(context.FieldSelection)
                        .Build();
                }
            }

            else if (!string.IsNullOrWhiteSpace(directive.Policy))
            {
                policy = await policyProvider.GetPolicyAsync(directive.Policy)
                    .ConfigureAwait(false);

                if (policy == null)
                {
                    context.Result = ErrorBuilder.New()
                        .SetMessage(string.Format(
                            CultureInfo.InvariantCulture,
                            AuthResources.AuthorizeMiddleware_PolicyNotFound,
                            directive.Policy))
                        .SetCode(AuthErrorCodes.PolicyNotFound)
                        .SetPath(context.Path)
                        .AddLocation(context.FieldSelection)
                        .Build();
                }
            }

            if (context.Result == null && policy != null)
            {
                AuthorizationResult result =
                    await authorizeService.AuthorizeAsync(
                        principal, context, policy)
                        .ConfigureAwait(false);
                return result.Succeeded;
            }

            return false;
        }
#endif
    }
}
