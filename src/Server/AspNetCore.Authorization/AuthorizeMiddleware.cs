using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
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

            if (context.ContextData.TryGetValue(
                nameof(ClaimsPrincipal), out var o)
                && o is ClaimsPrincipal p)
            {
                principal = p;
                allowed = p.Identity.IsAuthenticated;
            }

            allowed = allowed && IsInRoles(principal, directive.Roles);

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
                // TODO : resources
                context.Result = ErrorBuilder.New()
                    .SetMessage(
                        "The current user is not authorized to " +
                        "access this resource.")
                    .SetCode(AuthErrorCodes.NotAuthorized)
                    .SetPath(context.Path)
                    .AddLocation(context.FieldSelection)
                    .Build();
            }
        }

        private static bool IsInRoles(
            IPrincipal principal,
            IReadOnlyCollection<string> roles)
        {
            if (roles != null)
            {
                foreach (var role in roles)
                {
                    if (!principal.IsInRole(role))
                    {
                        return false;
                    }
                }
            }

            return true;
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
                    // TODO : resources
                    context.Result = context.Result = ErrorBuilder.New()
                        .SetMessage(
                            "The default authorization policy does not exist.")
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
                    // TODO : resources
                    context.Result = ErrorBuilder.New()
                        .SetMessage(
                            $"The `{directive.Policy}` authorization policy " +
                            "does not exist.")
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
