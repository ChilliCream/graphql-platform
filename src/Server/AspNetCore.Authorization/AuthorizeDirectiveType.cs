using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;

#if !ASPNETCLASSIC
using Microsoft.AspNetCore.Authorization;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic.Authorization
#else
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

            descriptor.Middleware(
                next => context => AuthorizeAsync(context, next));
        }

        private static async Task AuthorizeAsync(
            IDirectiveContext context,
            DirectiveDelegate next)
        {
#if !ASPNETCLASSIC
            IAuthorizationService authorizeService = context
                .Service<IAuthorizationService>();
            IAuthorizationPolicyProvider policyProvider = context
                .Service<IAuthorizationPolicyProvider>();
#endif
            ClaimsPrincipal principal = context
                .CustomProperty<ClaimsPrincipal>(nameof(ClaimsPrincipal));
            AuthorizeDirective directive = context.Directive
                .ToObject<AuthorizeDirective>();
            var allowed = IsInRoles(principal, directive.Roles);

#if !ASPNETCLASSIC
            if (allowed)
            {
                AuthorizationPolicy policy =
                    string.IsNullOrWhiteSpace(directive.Policy)
                        ? await policyProvider.GetDefaultPolicyAsync()
                        : await policyProvider.GetPolicyAsync(directive.Policy);

                if (policy == null)
                {
                    context.Result = QueryErro
                    return;
                }

                AuthorizationResult result =
                    await authorizeService.AuthorizeAsync(principal, policy);

                allowed = result.Succeeded;
            }
#endif

            if (allowed)
            {
                await next(context);
            }
            else
            {
                context.Result = QueryError.CreateFieldError(
                    "The current user is not authorized to " +
                    "access this resource.",
                    context.Path,
                    context.FieldSelection);
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
    }
}
