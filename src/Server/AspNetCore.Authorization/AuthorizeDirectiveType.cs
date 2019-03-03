using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
#if ASPNETCLASSIC

namespace HotChocolate.AspNetClassic.Authorization
#else
using Microsoft.AspNetCore.Authorization;

namespace HotChocolate.AspNetCore.Authorization
#endif
{
    public class AuthorizeDirectiveType
        : DirectiveType<AuthorizeDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<AuthorizeDirective> descriptor)
        {
            descriptor.Name("authorize");

            descriptor.Location(DirectiveLocation.Object)
                .Location(DirectiveLocation.FieldDefinition);

            descriptor.Repeatable();

            descriptor.Middleware(
                next => context => AuthorizeAsync(context, next));
        }

        private static async Task AuthorizeAsync(
            IDirectiveContext context,
            FieldDelegate next)
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
                allowed = true;
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
                await next(context).ConfigureAwait(false);
            }
            else if (context.Result == null)
            {
                context.Result = ErrorBuilder.New()
                    .SetMessage(
                        "The current user is not authorized to " +
                        "access this resource.")
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
            IAuthorizationService authorizeService = context
                .Service<IAuthorizationService>();
            IAuthorizationPolicyProvider policyProvider = context
                .Service<IAuthorizationPolicyProvider>();

            AuthorizationPolicy policy = null;

            if (directive.Roles.Count == 0
                && string.IsNullOrWhiteSpace(directive.Policy))
            {
                policy = await policyProvider.GetDefaultPolicyAsync()
                    .ConfigureAwait(false);
                if (policy == null)
                {
                    context.Result = QueryError.CreateFieldError(
                        "The default authorization policy does not exist.",
                        context.FieldSelection);
                }
            }

            else if (!string.IsNullOrWhiteSpace(directive.Policy))
            {
                policy = await policyProvider.GetPolicyAsync(directive.Policy)
                    .ConfigureAwait(false);

                if (policy == null)
                {
                    context.Result = QueryError.CreateFieldError(
                        $"The `{directive.Policy}` authorization policy " +
                        "does not exist.",
                        context.FieldSelection);
                }
            }

            if (context.Result == null && policy != null)
            {
                AuthorizationResult result =
                await authorizeService.AuthorizeAsync(principal, policy)
                    .ConfigureAwait(false);
                return result.Succeeded;
            }

            return false;
        }
#endif
    }
}
